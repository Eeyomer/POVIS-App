import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ViolatorsService } from '../../core/services/violators-service';
import { ToastService } from '../../shared/toast/toast-service';
import { ViolatorDTO, ViolatorHistoryDTO } from '../../core/models/app-dto';

@Component({
  selector: 'app-verify',
  imports: [CommonModule, FormsModule],
  templateUrl: './verify.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Verify {
  search = signal('');
  results = signal<ViolatorDTO[]>([]);
  searching = signal(false);

  selected = signal<ViolatorDTO | null>(null);
  history = signal<ViolatorHistoryDTO[]>([]);
  loadingHistory = signal(false);

  constructor(
    private violatorsService: ViolatorsService,
    private toast: ToastService,
  ) {}

  onSearchChange(value: string): void {
    this.search.set(value);
    this.selected.set(null);

    if (!value.trim()) {
      this.results.set([]);
      return;
    }

    this.searching.set(true);
    this.violatorsService.search(value, 1, 20).subscribe((res) => {
      this.results.set(res.items);
      this.searching.set(false);
    });
  }

  select(violator: ViolatorDTO): void {
    this.selected.set(violator);
    this.loadingHistory.set(true);
    this.violatorsService.getHistory(violator.violatorId).subscribe({
      next: (h) => {
        this.history.set(h);
        this.loadingHistory.set(false);
      },
      error: (err) => {
        this.toast.error(err.message);
        this.loadingHistory.set(false);
      },
    });
  }

  clear(): void {
    this.selected.set(null);
    this.history.set([]);
  }

  complianceLabel(status: string): string {
    switch (status) {
      case 'Paid':
      case 'Closed':
        return 'Compliant';
      case 'DidNotComply':
        return 'Did Not Comply';
      case 'Pending':
      case 'ForSeminar':
      case 'ForCounseling':
      case 'ForCommunityService':
        return 'Pending Compliance';
      case 'CaseFiled':
        return 'Case Filed';
      case 'Dismissed':
        return 'Dismissed';
      case 'WarningIssued':
        return 'Warning Only';
      default:
        return status;
    }
  }
}
