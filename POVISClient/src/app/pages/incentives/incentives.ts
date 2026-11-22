import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { SkeletonRows } from '../../shared/components/skeleton/skeleton';
import { Pagination } from '../../shared/components/pagination/pagination';
import { AuthService } from '../../core/services/auth-service';
import { IncentivesService } from '../../core/services/incentives-service';
import { ViolationsService } from '../../core/services/violations-service';
import { ToastService } from '../../shared/toast/toast-service';
import { manilaToday } from '../../core/utils/manila-time';
import { DIVISIONS, Division, IncentiveList, IncentivePeriod, IncentiveRow, OfficerOption } from '../../core/models/app-dto';

const EMPTY: IncentiveList = {
  period: 'monthly', from: '', to: '', totalCount: 0, totalAmount: 0, totalIncentive: 0,
  page: 1, pageSize: 20, items: [], officerTotals: [],
};

const PERIODS: { value: IncentivePeriod; label: string }[] = [
  { value: 'daily', label: 'Daily' },
  { value: 'weekly', label: 'Weekly' },
  { value: 'monthly', label: 'Monthly' },
  { value: 'annual', label: 'Annual' },
  { value: 'custom', label: 'Custom' },
];

/** yyyy-MM-dd arithmetic done in UTC so the browser's timezone never shifts the day. */
function shift(date: string, period: IncentivePeriod, direction: 1 | -1): string {
  const [y, m, d] = date.split('-').map(Number);
  const dt = new Date(Date.UTC(y, m - 1, d));
  if (period === 'daily') dt.setUTCDate(dt.getUTCDate() + direction);
  else if (period === 'weekly') dt.setUTCDate(dt.getUTCDate() + 7 * direction);
  else if (period === 'monthly') {
    dt.setUTCDate(1);
    dt.setUTCMonth(dt.getUTCMonth() + direction);
  } else if (period === 'annual') dt.setUTCFullYear(dt.getUTCFullYear() + direction);
  return dt.toISOString().slice(0, 10);
}

/**
 * Incentives earned by apprehending officers on paid OVRs. MIS Personnel / OVR Supervisor see every officer
 * (with an officer filter); an Apprehending Officer sees only their own. Rates are frozen per payment on the server.
 */
@Component({
  selector: 'app-incentives',
  imports: [CommonModule, FormsModule, SkeletonRows, Pagination],
  templateUrl: './incentives.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Incentives implements OnInit {
  private service = inject(IncentivesService);
  private violationsService = inject(ViolationsService);
  private toast = inject(ToastService);
  auth = inject(AuthService);

  /** The signed-in Apprehending Officer's own view (no officer filter, own data only). */
  mine = this.auth.isApprehendingOfficer();

  periods = PERIODS;
  divisions = DIVISIONS;

  loading = signal(true);
  data = signal<IncentiveList>(EMPTY);
  officers = signal<OfficerOption[]>([]);
  expanded = signal<string | null>(null);

  period = signal<IncentivePeriod>('monthly');
  date = signal(manilaToday());
  from = signal(manilaToday());
  to = signal(manilaToday());
  officerId = signal('');
  division = signal<Division | ''>('');
  search = signal('');
  page = signal(1);
  pageSize = 20;

  rangeLabel = computed(() => {
    const d = this.data();
    if (!d.from) return '';
    return d.from === d.to ? this.pretty(d.from) : `${this.pretty(d.from)} – ${this.pretty(d.to)}`;
  });

  ngOnInit(): void {
    if (!this.mine) this.violationsService.getOfficers().subscribe((o) => this.officers.set(o));
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.service
      .list(
        {
          period: this.period(),
          date: this.date(),
          from: this.from(),
          to: this.to(),
          officerId: this.officerId() || null,
          division: this.division() || null,
          search: this.search().trim() || null,
          page: this.page(),
          pageSize: this.pageSize,
        },
        this.mine,
      )
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (d) => this.data.set(d),
        error: (err: Error) => {
          this.data.set(EMPTY);
          this.toast.error(err.message);
        },
      });
  }

  private reload(): void {
    this.page.set(1);
    this.expanded.set(null);
    this.refresh();
  }

  setPeriod(p: IncentivePeriod): void {
    this.period.set(p);
    this.reload();
  }

  onDateChange(value: string): void {
    if (!value) return;
    this.date.set(value);
    this.reload();
  }

  onRangeChange(which: 'from' | 'to', value: string): void {
    if (!value) return;
    (which === 'from' ? this.from : this.to).set(value);
    if (this.from() > this.to()) {
      this.toast.error('The From date cannot be after the To date.');
      return;
    }
    this.reload();
  }

  step(direction: 1 | -1): void {
    this.date.set(shift(this.date(), this.period(), direction));
    this.reload();
  }

  goToToday(): void {
    this.date.set(manilaToday());
    this.reload();
  }

  onOfficerChange(value: string): void {
    this.officerId.set(value);
    this.reload();
  }

  onDivisionChange(value: string): void {
    this.division.set(value as Division | '');
    this.reload();
  }

  onSearchChange(value: string): void {
    this.search.set(value);
    this.reload();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.expanded.set(null);
    this.refresh();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.reload();
  }

  toggle(row: IncentiveRow): void {
    this.expanded.update((id) => (id === row.paymentId ? null : row.paymentId));
  }

  /** Show only one officer from the breakdown table. */
  filterOfficer(id?: string | null): void {
    if (id) this.onOfficerChange(id);
  }

  private pretty(iso: string): string {
    const [y, m, d] = iso.split('-').map(Number);
    return new Date(Date.UTC(y, m - 1, d)).toLocaleDateString('en-PH', {
      year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC',
    });
  }
}
