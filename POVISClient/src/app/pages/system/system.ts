import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ViewModeService } from '../../core/services/view-mode-service';
import { SystemService } from '../../core/services/system-service';
import { ToastService } from '../../shared/toast/toast-service';
import { SystemHealth, TechnicalLog } from '../../core/models/app-dto';
import { PagedResult } from '../../core/models/app-model';

const EMPTY_LOGS: PagedResult<TechnicalLog> = { items: [], totalCount: 0, page: 1, pageSize: 50 };

@Component({
  selector: 'app-system',
  imports: [CommonModule, FormsModule],
  templateUrl: './system.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class System implements OnInit {
  health = signal<SystemHealth | null>(null);
  logsPage = signal<PagedResult<TechnicalLog>>(EMPTY_LOGS);
  logsLoading = signal(false);
  restoring = signal(false);

  searchTerm = signal('');
  methodFilter = signal('');
  page = signal(1);
  pageSize = 25;

  selectedFile: File | null = null;
  showRestoreConfirm = signal(false);

  constructor(
    private systemService: SystemService,
    private toast: ToastService,
    public viewMode: ViewModeService,
  ) {}

  onModalFormsChange(enabled: boolean): void {
    this.viewMode.setModalForms(enabled).subscribe({
      next: () => this.toast.success(enabled ? 'Modal forms enabled for all users.' : 'Modal forms disabled for all users.'),
      error: (err) => this.toast.error(err?.error?.message || 'Could not update the setting.'),
    });
  }

  ngOnInit(): void {
    this.refreshHealth();
    this.refreshLogs();
  }

  refreshHealth(): void {
    this.systemService.health().subscribe({
      next: (h) => this.health.set(h),
      error: (err) => this.toast.error(err.message),
    });
  }

  refreshLogs(): void {
    this.logsLoading.set(true);
    this.systemService
      .logs({
        search: this.searchTerm() || null,
        method: this.methodFilter() || null,
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (page) => {
          this.logsPage.set(page);
          this.logsLoading.set(false);
        },
        error: (err) => {
          this.toast.error(err.message);
          this.logsLoading.set(false);
        },
      });
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    this.page.set(1);
    this.refreshLogs();
  }

  onMethodChange(value: string): void {
    this.methodFilter.set(value);
    this.page.set(1);
    this.refreshLogs();
  }

  nextPage(): void {
    const p = this.logsPage();
    if (p.page * p.pageSize >= p.totalCount) return;
    this.page.set(p.page + 1);
    this.refreshLogs();
  }

  prevPage(): void {
    if (this.page() <= 1) return;
    this.page.set(this.page() - 1);
    this.refreshLogs();
  }

  downloadBackup(): void {
    this.systemService.downloadBackup();
    this.toast.success('Backup download started.');
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  openRestoreConfirm(): void {
    if (!this.selectedFile) {
      this.toast.error('Select a backup file first.');
      return;
    }
    this.showRestoreConfirm.set(true);
  }

  cancelRestore(): void {
    this.showRestoreConfirm.set(false);
  }

  confirmRestore(): void {
    if (!this.selectedFile) return;
    this.restoring.set(true);
    this.systemService.restore(this.selectedFile).subscribe({
      next: () => {
        this.toast.success('System restored from backup.');
        this.restoring.set(false);
        this.showRestoreConfirm.set(false);
        this.selectedFile = null;
        this.refreshHealth();
        this.refreshLogs();
      },
      error: (err) => {
        this.toast.error(err.message);
        this.restoring.set(false);
      },
    });
  }
}
