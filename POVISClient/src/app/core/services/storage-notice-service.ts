import { Injectable, signal } from '@angular/core';

export const STORAGE_FULL_MESSAGE =
  'Neon free storage is consumed. Adding new records is disabled — please subscribe to avail more storage.';

/** Drives the "storage is consumed" modal shown when an add is attempted while IS_STORAGE_FULL is on. */
@Injectable({ providedIn: 'root' })
export class StorageNoticeService {
  readonly open = signal(false);

  show(): void {
    this.open.set(true);
  }

  dismiss(): void {
    this.open.set(false);
  }
}
