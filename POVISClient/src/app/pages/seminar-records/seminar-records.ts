import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormPanel } from '../../shared/components/form-panel/form-panel';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SeminarRecordsService } from '../../core/services/seminar-records-service';
import { ViolationsService } from '../../core/services/violations-service';
import { AuthService } from '../../core/services/auth-service';
import { Pagination } from '../../shared/components/pagination/pagination';
import { BulkUpload } from '../../shared/components/bulk-upload/bulk-upload';
import { ComplianceStatus, SeminarRecordDTO, SeminarType, ViolationDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-seminar-records',
  imports: [CommonModule, ReactiveFormsModule, BulkUpload, Pagination, FormPanel],
  templateUrl: './seminar-records.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeminarRecords implements OnInit {
  records = signal<SeminarRecordDTO[]>([]);
  totalCount = signal(0);
  page = signal(1);
  readonly pageSize = 20;
  violations = signal<ViolationDTO[]>([]);

  showForm = signal(false);
  editing = signal<SeminarRecordDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    violationId: ['', Validators.required],
    seminarType: ['SmokingCessation' as SeminarType, Validators.required],
    scheduledDate: [''],
    requiredDays: [null as number | null],
    status: ['Scheduled' as ComplianceStatus],
    dateCompleted: [''],
    remarks: [''],
  });

  constructor(
    private recordsService: SeminarRecordsService,
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
    this.recordsService.list(null, null, this.page(), this.pageSize, this.search() || null).subscribe((result) => {
      this.records.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ seminarType: 'SmokingCessation', status: 'Scheduled' });
    this.showForm.set(true);
  }

  openEdit(item: SeminarRecordDTO): void {
    this.editing.set(item);
    this.form.reset({
      violationId: item.violationId,
      seminarType: item.seminarType,
      scheduledDate: item.scheduledDate ?? '',
      requiredDays: item.requiredDays ?? null,
      status: item.status,
      dateCompleted: item.dateCompleted ?? '',
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
      ? this.recordsService.update(current.seminarRecordId, {
          seminarRecordId: current.seminarRecordId,
          seminarType: raw.seminarType!,
          scheduledDate: raw.scheduledDate || null,
          requiredDays: raw.requiredDays,
          status: raw.status!,
          dateCompleted: raw.dateCompleted || null,
          remarks: raw.remarks || null,
        })
      : this.recordsService.create({
          violationId: raw.violationId!,
          seminarType: raw.seminarType!,
          scheduledDate: raw.scheduledDate || null,
          requiredDays: raw.requiredDays,
          remarks: raw.remarks || null,
        });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Seminar record updated.' : 'Seminar record added.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  remove(item: SeminarRecordDTO): void {
    if (!confirm(`Delete seminar record for ${item.ovrNumber}?`)) return;
    this.recordsService.remove(item.seminarRecordId).subscribe({
      next: () => {
        this.toast.success('Seminar record deleted.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }
}
