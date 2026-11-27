import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { Skeleton, SkeletonRows } from '../../shared/components/skeleton/skeleton';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ViewModeService } from '../../core/services/view-mode-service';
import { SystemService } from '../../core/services/system-service';
import { ToastService } from '../../shared/toast/toast-service';
import { BackupModule, SystemHealth, TechnicalLog } from '../../core/models/app-dto';
import { PagedResult } from '../../core/models/app-model';

const EMPTY_LOGS: PagedResult<TechnicalLog> = { items: [], totalCount: 0, page: 1, pageSize: 50 };

@Component({
  selector: 'app-system',
  imports: [Skeleton, SkeletonRows, CommonModule, FormsModule],
  templateUrl: './system.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class System implements OnInit {
  health = signal<SystemHealth | null>(null);
  logsPage = signal<PagedResult<TechnicalLog>>(EMPTY_LOGS);
  logsLoading = signal(true);

  backupModules = signal<BackupModule[]>([]);
  modulesLoading = signal(true);
  selectedFiles = signal<Record<string, File | null>>({});
  confirmKey = signal<string | null>(null);
  restoringKey = signal<string | null>(null);
  downloadingKey = signal<string | null>(null);

  searchTerm = signal('');
  methodFilter = signal('');
  page = signal(1);
  pageSize = 25;

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

  // ----- Incentive rate -----
  incentiveRate = signal<number | null>(null);
  incentiveRateInput = '';
  savingRate = signal(false);

  loadIncentiveRate(): void {
    this.systemService.incentiveRate().subscribe({
      next: (rate) => {
        this.incentiveRate.set(rate);
        this.incentiveRateInput = String(rate);
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  saveIncentiveRate(): void {
    const rate = Number(this.incentiveRateInput);
    if (this.incentiveRateInput.trim() === '' || !Number.isFinite(rate) || rate < 0 || rate > 100) {
      this.toast.error('Enter a percentage between 0 and 100.');
      return;
    }
    this.savingRate.set(true);
    this.systemService.setIncentiveRate(rate).subscribe({
      next: (saved) => {
        this.incentiveRate.set(saved);
        this.incentiveRateInput = String(saved);
        this.savingRate.set(false);
        this.toast.success(`Incentive rate set to ${saved}%. It applies to payments recorded from now on.`);
      },
      error: (err) => {
        this.savingRate.set(false);
        this.toast.error(err.message);
      },
    });
  }

  ngOnInit(): void {
    this.loadIncentiveRate();
    this.refreshHealth();
    this.loadBackupModules();
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

  onPageSizeChange(size: number): void {
    this.pageSize = size;
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

  // ----- Per-module backup & restore -----
  loadBackupModules(): void {
    this.modulesLoading.set(true);
    this.systemService.backupModules().subscribe({
      next: (modules) => {
        this.backupModules.set(modules);
        this.modulesLoading.set(false);
      },
      error: (err) => {
        this.toast.error(err.message);
        this.modulesLoading.set(false);
      },
    });
  }

  downloadBackup(module: BackupModule): void {
    this.downloadingKey.set(module.key);
    this.systemService.downloadBackup(module.key).subscribe({
      next: () => {
        this.downloadingKey.set(null);
        this.toast.success(`${module.name} backup downloaded.`);
      },
      error: (err) => {
        this.downloadingKey.set(null);
        this.toast.error(err.message);
      },
    });
  }

  onFileSelected(module: BackupModule, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFiles.update((files) => ({ ...files, [module.key]: file }));
    if (this.confirmKey() === module.key && !file) this.confirmKey.set(null);
    input.value = '';
  }

  selectedFile(key: string): File | null {
    return this.selectedFiles()[key] ?? null;
  }

  openRestoreConfirm(module: BackupModule): void {
    if (!this.selectedFile(module.key)) {
      this.toast.error(`Select a ${module.name} backup file first.`);
      return;
    }
    this.confirmKey.set(module.key);
  }

  cancelRestore(): void {
    this.confirmKey.set(null);
  }

  confirmRestore(module: BackupModule): void {
    const file = this.selectedFile(module.key);
    if (!file) return;
    this.restoringKey.set(module.key);
    this.systemService.restore(module.key, file).subscribe({
      next: (count) => {
        this.toast.success(`${module.name} restored from backup (${count} record${count === 1 ? '' : 's'}).`);
        this.restoringKey.set(null);
        this.confirmKey.set(null);
        this.selectedFiles.update((files) => ({ ...files, [module.key]: null }));
        this.loadBackupModules();
        this.refreshHealth();
        this.refreshLogs();
      },
      error: (err) => {
        this.toast.error(err.message);
        this.restoringKey.set(null);
      },
    });
  }
}
