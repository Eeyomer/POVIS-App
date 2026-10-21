import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PaymentsService } from '../../core/services/payments-service';
import { ViolationsService } from '../../core/services/violations-service';
import { AuthService } from '../../core/services/auth-service';
import { Pagination } from '../../shared/components/pagination/pagination';
import { BulkUpload } from '../../shared/components/bulk-upload/bulk-upload';
import { PaymentDTO, ViolationDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-payments',
  imports: [CommonModule, ReactiveFormsModule, BulkUpload, Pagination],
  templateUrl: './payments.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Payments implements OnInit {
  payments = signal<PaymentDTO[]>([]);
  totalCount = signal(0);
  page = signal(1);
  readonly pageSize = 20;
  violations = signal<ViolationDTO[]>([]);

  showForm = signal(false);
  editing = signal<PaymentDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    violationId: ['', Validators.required],
    dateOfPayment: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    receiptNumber: [''],
    paidAt: [''],
    remarks: [''],
  });

  constructor(
    private paymentsService: PaymentsService,
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
    this.paymentsService.list(null, null, this.page(), this.pageSize, this.search() || null).subscribe((result) => {
      this.payments.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ amount: 0 });
    this.showForm.set(true);
  }

  openEdit(item: PaymentDTO): void {
    this.editing.set(item);
    this.form.reset({
      violationId: item.violationId,
      dateOfPayment: item.dateOfPayment,
      amount: item.amount,
      receiptNumber: item.receiptNumber ?? '',
      paidAt: item.paidAt ?? '',
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
      ? this.paymentsService.update(current.paymentId, {
          paymentId: current.paymentId,
          dateOfPayment: raw.dateOfPayment!,
          amount: raw.amount!,
          receiptNumber: raw.receiptNumber || null,
          paidAt: raw.paidAt || null,
          remarks: raw.remarks || null,
        })
      : this.paymentsService.create({
          violationId: raw.violationId!,
          dateOfPayment: raw.dateOfPayment!,
          amount: raw.amount!,
          receiptNumber: raw.receiptNumber || null,
          paidAt: raw.paidAt || null,
          remarks: raw.remarks || null,
        });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Payment updated.' : 'Payment recorded.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  remove(item: PaymentDTO): void {
    if (!confirm(`Void payment for ${item.ovrNumber}?`)) return;
    this.paymentsService.remove(item.paymentId).subscribe({
      next: () => {
        this.toast.success('Payment voided.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }
}
