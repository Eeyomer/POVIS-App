import { ChangeDetectionStrategy, Component, HostListener, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Skeleton, SkeletonRows } from '../../shared/components/skeleton/skeleton';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportsService } from '../../core/services/reports-service';
import { ToastService } from '../../shared/toast/toast-service';
import { BarangayDTO, DIVISIONS, OfficerOption, OrdinanceSummaryReport, PerBarangayReport, ReportExportFormat, ReportExportType } from '../../core/models/app-dto';
import { BarangaysService } from '../../core/services/barangays-service';
import { ViolationsService } from '../../core/services/violations-service';
import { manilaFirstDayOfMonth, manilaToday } from '../../core/utils/manila-time';

function firstDayOfMonth(): string {
  return manilaFirstDayOfMonth();
}

function today(): string {
  return manilaToday();
}

@Component({
  selector: 'app-reports',
  imports: [Skeleton, SkeletonRows, CommonModule, FormsModule],
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
  officers = signal<OfficerOption[]>([]);
  /** Apprehending officers ticked in the filter (empty = all officers). */
  selectedOfficerIds = signal<string[]>([]);
  officerMenuOpen = signal(false);

  readonly exportOptions: { value: ReportExportType; label: string }[] = [
    { value: 'overall', label: 'Overall (ordinance summary)' },
    { value: 'per-barangay', label: '% per Barangay & per Ordinance' },
    { value: 'seminar', label: 'Talakayan Seminar per Barangay' },
    { value: 'incentives', label: 'Incentives (10% per deputized enforcer)' },
    { value: 'enforcers', label: 'Deputized Enforcers' },
    { value: 'apprehension', label: 'Apprehension Report' },
  ];
  readonly divisions = DIVISIONS;

  constructor(
    private reportsService: ReportsService,
    private toast: ToastService,
    private barangaysService: BarangaysService,
    private sanitizer: DomSanitizer,
    private violationsService: ViolationsService,
  ) {
    this.barangaysService.list().subscribe((list) => this.barangays.set([...list].sort((a, b) => a.name.localeCompare(b.name))));
    this.violationsService.getOfficers().subscribe({
      next: (list) => this.officers.set(list),
      error: () => {},
    });
    this.runOrdinanceSummary();
  }

  isOfficerSelected(id: string): boolean {
    return this.selectedOfficerIds().includes(id);
  }

  toggleOfficer(id: string): void {
    this.selectedOfficerIds.update((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
  }

  clearOfficers(): void {
    this.selectedOfficerIds.set([]);
  }

  /** Text on the officer filter button. */
  officerFilterLabel(): string {
    const ids = this.selectedOfficerIds();
    if (ids.length === 0) return 'All officers';
    const names = this.officers().filter((o) => ids.includes(o.id)).map((o) => o.name);
    return names.length <= 2 ? names.join(', ') : `${names.length} officers selected`;
  }

  setFrom(value: string): void {
    this.from.set(value);
  }

  setTo(value: string): void {
    this.to.set(value);
  }

  /** Report currently shown in the preview modal. */
  preview = signal<{ url: SafeResourceUrl; rawUrl: string; file: { blob: Blob; fileName: string }; type: ReportExportType; params: Record<string, string> } | null>(null);
  previewing = signal(false);

  private currentParams(): { type: ReportExportType; params: Record<string, string> } {
    const type = this.exportType();
    const params: Record<string, string> = { from: this.from(), to: this.to() };
    if (type === 'seminar' && this.exportBarangayId()) params['barangayId'] = this.exportBarangayId();
    if (type === 'incentives' && this.exportDivision()) params['division'] = this.exportDivision();
    if (type === 'apprehension' && this.selectedOfficerIds().length) params['officerIds'] = this.selectedOfficerIds().join(',');
    return { type, params };
  }

  /** Generates the report as PDF and shows it before anything is downloaded. */
  openPreview(): void {
    const { type, params } = this.currentParams();
    this.previewing.set(true);
    this.reportsService.fetchExport(type, 'pdf', params).subscribe({
      next: (file) => {
        const rawUrl = URL.createObjectURL(file.blob);
        this.preview.set({ url: this.sanitizer.bypassSecurityTrustResourceUrl(rawUrl), rawUrl, file, type, params });
        this.previewing.set(false);
      },
      error: (err) => {
        this.toast.error(err.message);
        this.previewing.set(false);
      },
    });
  }

  closePreview(): void {
    const p = this.preview();
    if (p) URL.revokeObjectURL(p.rawUrl);
    this.preview.set(null);
  }

  /** Downloads from the preview modal. The PDF is already generated; Excel is fetched on demand. */
  download(format: ReportExportFormat): void {
    const p = this.preview();
    if (!p) return;
    if (format === 'pdf') {
      this.reportsService.save(p.file);
      return;
    }
    this.exporting.set(format);
    this.reportsService.download(p.type, format, p.params).subscribe({
      next: () => this.exporting.set(null),
      error: (err) => {
        this.toast.error(err.message);
        this.exporting.set(null);
      },
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.preview()) this.closePreview();
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
