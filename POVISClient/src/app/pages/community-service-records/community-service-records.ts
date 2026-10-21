import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommunityServiceRecordsService } from '../../core/services/community-service-records-service';
import { ViolationsService } from '../../core/services/violations-service';
import { AuthService } from '../../core/services/auth-service';
import { Pagination } from '../../shared/components/pagination/pagination';
import { BulkUpload } from '../../shared/components/bulk-upload/bulk-upload';
import { CommunityServiceDTO, ComplianceStatus, ViolationDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-community-service-records',
  imports: [CommonModule, ReactiveFormsModule, BulkUpload, Pagination],
  templateUrl: './community-service-records.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityServiceRecords implements OnInit {
  records = signal<CommunityServiceDTO[]>([]);
  totalCount = signal(0);
  page = signal(1);
  readonly pageSize = 20;
  violations = signal<ViolationDTO[]>([]);

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
  ) {}

  ngOnInit(): void {
    this.violationsService.list({ pageSize: 200 }).subscribe((res) => this.violations.set(res.items));
    this.refresh();
  }

  search = signal('');

  onSearchChange(value: string): void {
    this.search.set(value);
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
}
