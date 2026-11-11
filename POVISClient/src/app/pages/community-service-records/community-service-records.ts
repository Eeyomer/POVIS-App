import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormPanel } from '../../shared/components/form-panel/form-panel';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommunityServiceRecordsService } from '../../core/services/community-service-records-service';
import { ViolationsService } from '../../core/services/violations-service';
import { AuthService } from '../../core/services/auth-service';
import { Pagination } from '../../shared/components/pagination/pagination';
import { BulkUpload } from '../../shared/components/bulk-upload/bulk-upload';
import { CommunityServiceDTO, ComplianceStatus, ViolationDTO } from '../../core/models/app-dto';
import { DocumentService, GeneratedDocument } from '../../core/services/document-service';
import { DocPreview } from '../../shared/components/doc-preview/doc-preview';
import { ToastService } from '../../shared/toast/toast-service';

import { SearchSelect, SelectOption } from '../../shared/components/search-select/search-select';

@Component({
  selector: 'app-community-service-records',
  imports: [CommonModule, ReactiveFormsModule, BulkUpload, Pagination, FormPanel, DocPreview, SearchSelect],
  templateUrl: './community-service-records.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityServiceRecords implements OnInit {
  records = signal<CommunityServiceDTO[]>([]);
  totalCount = signal(0);
  page = signal(1);
  pageSize = 20;
  violations = signal<ViolationDTO[]>([]);

  preview = signal<GeneratedDocument | null>(null);
  generatingId = signal<string | null>(null);

  showForm = signal(false);
  editing = signal<CommunityServiceDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    violationId: ['', Validators.required],
    requiredHours: [null as number | null],
    requiredDays: [null as number | null],
    scheduledDate: [''],
    status: ['Scheduled' as ComplianceStatus],
    dateCompleted: [''],
    assignedLocation: [''],
    remarks: [''],
  });

  constructor(
    private recordsService: CommunityServiceRecordsService,
    private violationsService: ViolationsService,
    public authService: AuthService,
    private toast: ToastService,
    private documents: DocumentService,
  ) {}

  ngOnInit(): void {
    this.searchViolations('');
    this.refresh();
  }

  search = signal('');

  onSearchChange(value: string): void {
    this.search.set(value);
    this.page.set(1);
    this.refresh();
  }

  violationOptions = computed<SelectOption[]>(() =>
    this.violations().map((v) => ({ value: v.violationId, label: `${v.ovrNumber} — ${v.violatorFullName}` })),
  );

  /** Loads the violations matching what was typed into the selector. */
  searchViolations(term: string): void {
    this.violationsService.list({ search: term || undefined, pageSize: 20 }).subscribe((res) => this.violations.set(res.items));
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page.set(1);
    this.refresh();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.refresh();
  }

  refresh(): void {
    this.recordsService.list(null, this.page(), this.pageSize, this.search() || null).subscribe((result) => {
      this.records.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ status: 'Scheduled' });
    this.showForm.set(true);
  }

  openEdit(item: CommunityServiceDTO): void {
    this.editing.set(item);
    this.form.reset({
      violationId: item.violationId,
      requiredHours: item.requiredHours ?? null,
      requiredDays: item.requiredDays ?? null,
      scheduledDate: item.scheduledDate ?? '',
      status: item.status,
      dateCompleted: item.dateCompleted ?? '',
      assignedLocation: item.assignedLocation ?? '',
      remarks: item.remarks ?? '',
    });
    this.showForm.set(true);
  }

  cancel(): void {
    this.showForm.set(false);
    this.editing.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    const current = this.editing();

    const request = current
      ? this.recordsService.update(current.communityServiceRecordId, {
          communityServiceRecordId: current.communityServiceRecordId,
          requiredHours: raw.requiredHours,
          requiredDays: raw.requiredDays,
          scheduledDate: raw.scheduledDate || null,
          status: raw.status!,
          dateCompleted: raw.dateCompleted || null,
          assignedLocation: raw.assignedLocation || null,
          remarks: raw.remarks || null,
        })
      : this.recordsService.create({
          violationId: raw.violationId!,
          requiredHours: raw.requiredHours,
          requiredDays: raw.requiredDays,
          scheduledDate: raw.scheduledDate || null,
          assignedLocation: raw.assignedLocation || null,
          remarks: raw.remarks || null,
        });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Community service record updated.' : 'Community service record added.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  remove(item: CommunityServiceDTO): void {
    if (!confirm(`Delete community service record for ${item.ovrNumber}?`)) return;
    this.recordsService.remove(item.communityServiceRecordId).subscribe({
      next: () => {
        this.toast.success('Community service record deleted.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  /** Fills the letter template with this record's data. */
  private generate(item: CommunityServiceDTO, onReady: (doc: GeneratedDocument) => void): void {
    this.generatingId.set(item.communityServiceRecordId);
    this.documents.communityService(item).then(
      (doc) => {
        this.generatingId.set(null);
        onReady(doc);
      },
      (err: Error) => {
        this.generatingId.set(null);
        this.toast.error(err.message);
      },
    );
  }

  previewDocument(item: CommunityServiceDTO): void {
    this.generate(item, (doc) => this.preview.set(doc));
  }

  downloadDocument(item: CommunityServiceDTO): void {
    this.generate(item, (doc) => this.documents.download(doc));
  }

  /** Certification of Compliance and Closure; only for violations that are already paid. */
  previewClosure(item: CommunityServiceDTO): void {
    if (!item.hasPayment) return;
    this.generatingId.set(item.communityServiceRecordId);
    this.documents.closure(item).then(
      (doc) => {
        this.generatingId.set(null);
        this.preview.set(doc);
      },
      (err: Error) => {
        this.generatingId.set(null);
        this.toast.error(err.message);
      },
    );
  }

  downloadPreviewed(): void {
    const doc = this.preview();
    if (doc) this.documents.download(doc);
  }
}
