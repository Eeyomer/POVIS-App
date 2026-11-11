import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { SkeletonRows } from '../../shared/components/skeleton/skeleton';
import { finalize } from 'rxjs';
import { FormPanel } from '../../shared/components/form-panel/form-panel';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FiledCasesService } from '../../core/services/filed-cases-service';
import { ViolationsService } from '../../core/services/violations-service';
import { AuthService } from '../../core/services/auth-service';
import { Pagination } from '../../shared/components/pagination/pagination';
import { BulkUpload } from '../../shared/components/bulk-upload/bulk-upload';
import { CaseStatus, FiledCaseDTO, ViolationDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

import { SearchSelect, SelectOption } from '../../shared/components/search-select/search-select';

@Component({
  selector: 'app-filed-cases',
  imports: [SkeletonRows, CommonModule, ReactiveFormsModule, BulkUpload, Pagination, FormPanel, SearchSelect],
  templateUrl: './filed-cases.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FiledCases implements OnInit {
  loading = signal(true);
  cases = signal<FiledCaseDTO[]>([]);
  totalCount = signal(0);
  page = signal(1);
  pageSize = 20;
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
    this.loading.set(true);
    this.filedCasesService.list(null, null, null, this.page(), this.pageSize, this.search() || null)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
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
