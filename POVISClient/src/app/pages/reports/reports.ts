import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportsService } from '../../core/services/reports-service';
import { ToastService } from '../../shared/toast/toast-service';
import { BarangayDTO, OrdinanceSummaryReport, PerBarangayReport, ReportExportFormat, ReportExportType } from '../../core/models/app-dto';
import { BarangaysService } from '../../core/services/barangays-service';
import { manilaFirstDayOfMonth, manilaToday } from '../../core/utils/manila-time';

function firstDayOfMonth(): string {
  return manilaFirstDayOfMonth();
}

function today(): string {
  return manilaToday();
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
  exportType = signal<ReportExportType>('overall');
  exportBarangayId = signal('');
  exportDivision = signal('');
  exporting = signal<ReportExportFormat | null>(null);
  barangays = signal<BarangayDTO[]>([]);

  readonly exportOptions: { value: ReportExportType; label: string }[] = [
    { value: 'overall', label: 'Overall (ordinance summary)' },
    { value: 'per-barangay', label: '% per Barangay & per Ordinance' },
    { value: 'seminar', label: 'Talakayan Seminar per Barangay' },
    { value: 'incentives', label: 'Incentives (10% per deputized enforcer)' },
    { value: 'enforcers', label: 'Deputized Enforcers' },
  ];
  readonly divisions = ['POD', 'BPD', 'ALD', 'PSD', 'PNP', 'SOG', 'Other'];

  constructor(
    private reportsService: ReportsService,
    private toast: ToastService,
    private barangaysService: BarangaysService,
  ) {
    this.barangaysService.list().subscribe((list) => this.barangays.set(list));
    this.runOrdinanceSummary();
  }

  setFrom(value: string): void {
    this.from.set(value);
  }

  setTo(value: string): void {
    this.to.set(value);
  }

  download(format: ReportExportFormat): void {
    const type = this.exportType();
    const params: Record<string, string> = { from: this.from(), to: this.to() };
    if (type === 'seminar' && this.exportBarangayId()) params['barangayId'] = this.exportBarangayId();
    if (type === 'incentives' && this.exportDivision()) params['division'] = this.exportDivision();

    this.exporting.set(format);
    this.reportsService.download(type, format, params).subscribe({
      next: () => this.exporting.set(null),
      error: (err) => {
        this.toast.error(err.message);
        this.exporting.set(null);
      },
    });
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
