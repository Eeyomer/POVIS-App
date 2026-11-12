import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { DocumentService, GeneratedDocument } from '../../core/services/document-service';
import { DocPreview } from '../../shared/components/doc-preview/doc-preview';
import { ToastService } from '../../shared/toast/toast-service';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { finalize, forkJoin } from 'rxjs';
import { BarangaysService } from '../../core/services/barangays-service';
import { ViolationsService } from '../../core/services/violations-service';
import { OrdinancesService } from '../../core/services/ordinances-service';
import { FiledCasesService } from '../../core/services/filed-cases-service';
import { AuthService } from '../../core/services/auth-service';
import { ReportsService } from '../../core/services/reports-service';
import { DashboardSummary } from '../../core/models/app-dto';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, Skeleton, DocPreview],
  templateUrl: './dashboard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard implements OnInit {
  loading = signal(true);
  barangayCount = signal(0);
  ordinanceCount = signal(0);
  pendingViolationCount = signal(0);
  openCaseCount = signal(0);

  /** Executive summary; only loaded for MIS Personnel and OVR Supervisor. */
  summary = signal<DashboardSummary | null>(null);
  summaryFailed = signal(false);

  /** Google Maps embed of the Pasig City Traffic and Parking Management Office. */
  readonly officeAddress =
    '2nd Floor, BPD Office, Tbar Building, F.Manalo St. Brgy. Sto Tomas, Pasig City (front of 711)';
  readonly mapUrl: SafeResourceUrl;
  readonly mapLink = 'https://www.google.com/maps/search/?api=1&query=' +
    encodeURIComponent('Pasig City Traffic and Parking Management Office, F. Manalo St, Sto. Tomas, Pasig City');

  preview = signal<GeneratedDocument | null>(null);
  previewingForm = signal<string | null>(null);

  constructor(
    sanitizer: DomSanitizer,
    private documents: DocumentService,
    private toast: ToastService,
    public authService: AuthService,
    private reportsService: ReportsService,
    private barangaysService: BarangaysService,
    private violationsService: ViolationsService,
    private ordinancesService: OrdinancesService,
    private filedCasesService: FiledCasesService,
  ) {
    const query = 'Pasig City Traffic and Parking Management Office, F. Manalo St, Sto. Tomas, Pasig City';
    this.mapUrl = sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.google.com/maps?q=${encodeURIComponent(query)}&output=embed`,
    );
  }

  /** Opens a blank form template in the preview modal. */
  previewForm(file: string, title: string): void {
    this.previewingForm.set(file);
    this.documents.template(file, title).then(
      (doc) => {
        this.previewingForm.set(null);
        this.preview.set(doc);
      },
      (err: Error) => {
        this.previewingForm.set(null);
        this.toast.error(err.message);
      },
    );
  }

  downloadPreviewed(): void {
    const doc = this.preview();
    if (doc) this.documents.download(doc);
  }

  ngOnInit(): void {
    if (this.authService.canApprove()) {
      this.reportsService.dashboard().subscribe({
        next: (data) => this.summary.set(data),
        error: () => this.summaryFailed.set(true),
      });
    }

    forkJoin({
      barangays: this.barangaysService.list(),
      ordinances: this.ordinancesService.list(),
      pendingViolations: this.violationsService.list({ status: 'Pending', pageSize: 1 }),
      filedCases: this.filedCasesService.list('Filed', undefined, undefined, 1, 1),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ barangays, ordinances, pendingViolations, filedCases }) => {
      this.barangayCount.set(barangays.length);
      this.ordinanceCount.set(ordinances.length);
      this.pendingViolationCount.set(pendingViolations.totalCount);
      this.openCaseCount.set(filedCases.totalCount);
    });
  }
}
