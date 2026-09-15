import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportsService } from '../../core/services/reports-service';
import { ToastService } from '../../shared/toast/toast-service';
import { OrdinanceSummaryReport, PerBarangayReport } from '../../core/models/app-dto';

function firstDayOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

@Component({
  selector: 'app-reports',
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Reports {
  from = signal(firstDayOfMonth());
  to = signal(today());

  ordinanceSummary = signal<OrdinanceSummaryReport | null>(null);
  perBarangay = signal<PerBarangayReport | null>(null);
  loading = signal(false);

  constructor(
    private reportsService: ReportsService,
    private toast: ToastService,
  ) {
    this.runOrdinanceSummary();
  }

  setFrom(value: string): void {
    this.from.set(value);
  }

  setTo(value: string): void {
    this.to.set(value);
  }

  runOrdinanceSummary(): void {
    this.loading.set(true);
    this.reportsService.ordinanceSummary(this.from(), this.to()).subscribe({
      next: (report) => {
        this.ordinanceSummary.set(report);
        this.loading.set(false);
      },
      error: (err) => {
        this.toast.error(err.message);
        this.loading.set(false);
      },
    });
  }

  runPerBarangay(): void {
    this.loading.set(true);
    this.reportsService.perBarangay(this.from(), this.to()).subscribe({
      next: (report) => {
        this.perBarangay.set(report);
        this.loading.set(false);
      },
      error: (err) => {
        this.toast.error(err.message);
        this.loading.set(false);
      },
    });
  }
}
