import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { AuthService } from '../../../core/services/auth-service';
import { BulkUploadService } from '../../../core/services/bulk-upload-service';
import { BulkImportResult } from '../../../core/models/app-dto';
import { ToastService } from '../../toast/toast-service';

/**
 * "Download Template" + "Bulk Upload" buttons for a module, with a result dialog.
 * Visible to MIS Personnel only (the API enforces the same rule).
 */
@Component({
  selector: 'app-bulk-upload',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (authService.canManageUsers()) {
    <button type="button" (click)="downloadTemplate()"
      class="px-4 py-2 rounded-lg border border-[#d3dae5] text-[#152238] text-[13px] font-semibold hover:bg-[#f3f5f8]">
      Download Template
    </button>
    <label
      class="px-4 py-2 rounded-lg border border-[#1e3a63] text-[#1e3a63] text-[13px] font-semibold hover:bg-[#f3f5f8] cursor-pointer"
      [class.opacity-50]="uploading()" [class.pointer-events-none]="uploading()">
      {{ uploading() ? 'Uploading...' : 'Bulk Upload' }}
      <input type="file" accept=".xlsx" class="hidden" (change)="onFileSelected($event)" [disabled]="uploading()" />
    </label>

    @if (result(); as r) {
    <div class="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[85vh] flex flex-col">
        <div class="flex items-center justify-between gap-3 px-6 pt-5 pb-3">
          <h2 class="text-[16px] font-semibold text-[#152238]">
            {{ r.errors.length ? title() + ' bulk upload failed - nothing was imported' : title() + ' bulk upload complete' }}
          </h2>
          <button type="button" (click)="dismiss()" class="text-[#7a889c] font-semibold text-[13px]">Close</button>
        </div>

        <div class="px-6 pb-5 overflow-y-auto">
          @if (r.errors.length) {
          <p class="text-[13px] text-[#7a889c] mb-2">Fix these rows in your file (row numbers match Excel) and upload again.</p>
          <ul class="space-y-1 text-[13px]">
            @for (e of r.errors; track $index) {
            <li class="text-red-600"><span class="font-semibold">Row {{ e.row }}:</span> {{ e.message }}</li>
            }
          </ul>
          } @else {
          <ul class="text-[13px] text-[#152238] grid grid-cols-1 sm:grid-cols-2 gap-2 mb-2">
            <li>Rows read: <b>{{ r.rowsRead }}</b></li>
            @for (c of r.counts; track c.label) {
            <li>{{ c.label }}: <b>{{ c.count }}</b></li>
            }
          </ul>
          @if (r.credentials.length) {
          <div class="mt-4 rounded-lg border border-amber-300 bg-amber-50 p-4">
            <p class="text-[13px] font-semibold text-amber-900 mb-1">Temporary passwords - shown only once</p>
            <p class="text-[12px] text-amber-900 mb-3">Save these now. Each user must change the password at first login.</p>
            <button type="button" (click)="downloadCredentials(r)"
              class="px-3 py-1.5 rounded-lg bg-[#1e3a63] text-white text-[12px] font-semibold hover:bg-[#284d82] mb-3">
              Download credentials (CSV)
            </button>
            <table class="w-full text-[12px]">
              <thead class="text-left text-[#7a889c]"><tr><th class="py-1">Username</th><th class="py-1">Temporary password</th></tr></thead>
              <tbody>
                @for (c of r.credentials; track c.username) {
                <tr class="border-t border-amber-200"><td class="py-1">{{ c.username }}</td><td class="py-1 font-mono">{{ c.temporaryPassword }}</td></tr>
                }
              </tbody>
            </table>
          </div>
          }
          }
        </div>
      </div>
    </div>
    }
    }
  `,
})
export class BulkUpload {
  /** API module key, e.g. "barangays", "violations", "seminar-records". */
  module = input.required<string>();
  /** Display name used in the dialog title. */
  title = input.required<string>();
  /** Emitted after a successful import so the page can reload its list. */
  imported = output<void>();

  uploading = signal(false);
  result = signal<BulkImportResult | null>(null);

  constructor(
    public authService: AuthService,
    private bulk: BulkUploadService,
    private toast: ToastService,
  ) {}

  downloadTemplate(): void {
    this.bulk.downloadTemplate(this.module()).subscribe({ error: (err: Error) => this.toast.error(err.message) });
  }

  onFileSelected(event: Event): void {
    const fileInput = event.target as HTMLInputElement;
    const file = fileInput.files?.[0];
    fileInput.value = '';
    if (!file) return;

    this.uploading.set(true);
    this.result.set(null);
    this.bulk.upload(this.module(), file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.result.set(result);
        this.toast.success(`${this.title()} imported.`);
        this.imported.emit();
      },
      error: (err: Error & { result?: BulkImportResult }) => {
        this.uploading.set(false);
        this.result.set(err.result ?? null);
        this.toast.error(err.message);
      },
    });
  }

  dismiss(): void {
    this.result.set(null);
  }

  downloadCredentials(r: BulkImportResult): void {
    const lines = ['username,temporaryPassword', ...r.credentials.map((c) => `${c.username},${c.temporaryPassword}`)];
    this.bulk.save(new Blob([lines.join('\r\n')], { type: 'text/csv' }), 'new-user-credentials.csv');
  }
}
