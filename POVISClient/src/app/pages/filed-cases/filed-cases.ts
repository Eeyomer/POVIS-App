import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FiledCasesService } from '../../core/services/filed-cases-service';
import { ViolationsService } from '../../core/services/violations-service';
import { AuthService } from '../../core/services/auth-service';
import { CaseStatus, FiledCaseDTO, ViolationDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-filed-cases',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './filed-cases.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FiledCases implements OnInit {
  cases = signal<FiledCaseDTO[]>([]);
  totalCount = signal(0);
  violations = signal<ViolationDTO[]>([]);

  showForm = signal(false);
  editing = signal<FiledCaseDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    violationId: ['', Validators.required],
    dateFiled: ['', Validators.required],
    npsDocketNumber: [''],
    status: ['Filed' as CaseStatus],
    dateResolved: [''],
    resolutionRemarks: [''],
    remarks: [''],
  });

  constructor(
    private filedCasesService: FiledCasesService,
    private violationsService: ViolationsService,
    public authService: AuthService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.violationsService.list({ pageSize: 200 }).subscribe((res) => this.violations.set(res.items));
    this.refresh();
  }

  refresh(): void {
    this.filedCasesService.list().subscribe((result) => {
      this.cases.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ status: 'Filed' });
    this.showForm.set(true);
  }

  openEdit(item: FiledCaseDTO): void {
    this.editing.set(item);
    this.form.reset({
      violationId: item.violationId,
      dateFiled: item.dateFiled,
      npsDocketNumber: item.npsDocketNumber ?? '',
      status: item.status,
      dateResolved: item.dateResolved ?? '',
      resolutionRemarks: item.resolutionRemarks ?? '',
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
      ? this.filedCasesService.update(current.filedCaseId, {
          filedCaseId: current.filedCaseId,
          dateFiled: raw.dateFiled!,
          npsDocketNumber: raw.npsDocketNumber || null,
          status: raw.status!,
          dateResolved: raw.dateResolved || null,
          resolutionRemarks: raw.resolutionRemarks || null,
          remarks: raw.remarks || null,
        })
      : this.filedCasesService.create({
          violationId: raw.violationId!,
          dateFiled: raw.dateFiled!,
          npsDocketNumber: raw.npsDocketNumber || null,
          remarks: raw.remarks || null,
        });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Filed case updated.' : 'Case filed.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  remove(item: FiledCaseDTO): void {
    if (!confirm(`Delete filed case for ${item.ovrNumber}?`)) return;
    this.filedCasesService.remove(item.filedCaseId).subscribe({
      next: () => {
        this.toast.success('Filed case deleted.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }
}
