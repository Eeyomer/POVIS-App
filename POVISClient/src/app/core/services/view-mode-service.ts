import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'povis.modalForms';

/** Whether create/edit/view panels open in a modal instead of inline. Toggled by MIS Personnel. */
@Injectable({ providedIn: 'root' })
export class ViewModeService {
  readonly modalForms = signal(this.load());

  toggle(): void {
    const next = !this.modalForms();
    this.modalForms.set(next);
    try {
      localStorage.setItem(STORAGE_KEY, next ? '1' : '0');
    } catch {
      // storage unavailable; the choice only lasts for this session
    }
  }

  private load(): boolean {
    try {
      return localStorage.getItem(STORAGE_KEY) === '1';
    } catch {
      return false;
    }
  }
}
