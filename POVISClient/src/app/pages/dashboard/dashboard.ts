import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { BarangaysService } from '../../core/services/barangays-service';
import { ViolationsService } from '../../core/services/violations-service';
import { OrdinancesService } from '../../core/services/ordinances-service';
import { FiledCasesService } from '../../core/services/filed-cases-service';
import { AuthService } from '../../core/services/auth-service';
import { ReportsService } from '../../core/services/reports-service';
import { DashboardSummary } from '../../core/models/app-dto';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard implements OnInit {
  barangayCount = signal(0);
  ordinanceCount = signal(0);
  pendingViolationCount = signal(0);
  openCaseCount = signal(0);

  /** Executive summary; only loaded for MIS Personnel and OVR Supervisor. */
  summary = signal<DashboardSummary | null>(null);
  summaryFailed = signal(false);

  constructor(
    public authService: AuthService,
    private reportsService: ReportsService,
    private barangaysService: BarangaysService,
    private violationsService: ViolationsService,
    private ordinancesService: OrdinancesService,
    private filedCasesService: FiledCasesService,
  ) {}

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
    }).subscribe(({ barangays, ordinances, pendingViolations, filedCases }) => {
      this.barangayCount.set(barangays.length);
      this.ordinanceCount.set(ordinances.length);
      this.pendingViolationCount.set(pendingViolations.totalCount);
      this.openCaseCount.set(filedCases.totalCount);
    });
  }
}
