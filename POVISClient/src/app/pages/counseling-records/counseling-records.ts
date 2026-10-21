import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CounselingRecordsService } from '../../core/services/counseling-records-service';
import { ViolationsService } from '../../core/services/violations-service';
import { AuthService } from '../../core/services/auth-service';
import { Pagination } from '../../shared/components/pagination/pagination';
import { BulkUpload } from '../../shared/components/bulk-upload/bulk-upload';
import { ComplianceStatus, CounselingDTO, ViolationDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-counseling-records',
  imports: [CommonModule, ReactiveFormsModule, BulkUpload, Pagination],
  templateUrl: './counseling-records.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CounselingRecords implements OnInit {
  records = signal<CounselingDTO[]>([]);
  totalCount = signal(0);
  page = signal(1);
  readonly pageSize = 20;
  violations = signal<ViolationDTO[]>([]);

  showForm = signal(false);
  editing = signal<CounselingDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    violationId: ['', Validators.required],
    interventionType: [''],
    conductedBy: [''],
    scheduledDate: [''],
    status: ['Scheduled' as ComplianceStatus],
    dateCompleted: [''],
    parentOrGuardianPresent: [false],
    remarks: [''],
  });

  constructor(
    private recordsService: CounselingRecordsService,
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
    this.form.reset({ status: 'Scheduled', parentOrGuardianPresent: false });
    this.showForm.set(true);
  }

  openEdit(item: CounselingDTO): void {
    this.editing.set(item);
    this.form.reset({
      violationId: item.violationId,
      interventionType: item.interventionType ?? '',
      conductedBy: item.conductedBy ?? '',
      scheduledDate: item.scheduledDate ?? '',
      status: item.status,
      dateCompleted: item.dateCompleted ?? '',
      parentOrGuardianPresent: item.parentOrGuardianPresent,
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
      ? this.recordsService.update(current.counselingRecordId, {
          counselingRecordId: current.counselingRecordId,
          interventionType: raw.interventionType || null,
          conductedBy: raw.conductedBy || null,
          scheduledDate: raw.scheduledDate || null,
          status: raw.status!,
          dateCompleted: raw.dateCompleted || null,
          parentOrGuardianPresent: !!raw.parentOrGuardianPresent,
          remarks: raw.remarks || null,
        })
      : this.recordsService.create({
          violationId: raw.violationId!,
          interventionType: raw.interventionType || null,
          conductedBy: raw.conductedBy || null,
          scheduledDate: raw.scheduledDate || null,
          parentOrGuardianPresent: !!raw.parentOrGuardianPresent,
          remarks: raw.remarks || null,
        });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Counseling record updated.' : 'Counseling record added.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  remove(item: CounselingDTO): void {
    if (!confirm(`Delete counseling record for ${item.ovrNumber}?`)) return;
    this.recordsService.remove(item.counselingRecordId).subscribe({
      next: () => {
        this.toast.success('Counseling record deleted.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }
}
