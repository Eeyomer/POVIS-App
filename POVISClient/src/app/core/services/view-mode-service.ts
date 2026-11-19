import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../environment/environment';
import { APIResponse } from '../models/app-model';

interface AppSettings {
  modalForms: boolean;
}

/**
 * System-wide setting (managed by MIS Personnel on the System page): when on, create/edit/view
 * panels open in a modal instead of inline for every user.
 *
 * Cached so the app does not wait on (or re-request) a rarely-changing setting:
 *  - the last known value is kept in localStorage and used immediately on the next page load, so the first
 *    screen already opens in the right mode instead of flashing the wrong one;
 *  - the server is asked again at most every 5 minutes (and always right after MIS Personnel changes it).
 */
@Injectable({ providedIn: 'root' })
export class ViewModeService {
  private static readonly STORAGE_KEY = 'povis_modal_forms';
  private static readonly REFRESH_AFTER_MS = 5 * 60 * 1000;

  private apiUrl = `${environment.apiUrl}/api/settings`;
  readonly modalForms = signal(this.readStored());
  private loadedAt = 0;

  constructor(private http: HttpClient) {}

  /** @param force skip the 5-minute freshness check and ask the server now. */
  load(force = false): void {
    if (!force && Date.now() - this.loadedAt < ViewModeService.REFRESH_AFTER_MS) return;
    this.http.get<APIResponse<AppSettings>>(this.apiUrl).subscribe({
      next: (res) => this.apply(!!res.data?.modalForms),
      error: () => {}, // keep the cached value; try again on the next load
    });
  }

  setModalForms(enabled: boolean): Observable<APIResponse<AppSettings>> {
    return this.http
      .put<APIResponse<AppSettings>>(this.apiUrl, { modalForms: enabled })
      .pipe(tap((res) => this.apply(!!res.data?.modalForms)));
  }

  private apply(value: boolean): void {
    this.modalForms.set(value);
    this.loadedAt = Date.now();
    try {
      localStorage.setItem(ViewModeService.STORAGE_KEY, String(value));
    } catch {
      // storage unavailable (private window, blocked): the in-memory value still works
    }
  }

  private readStored(): boolean {
    try {
      return localStorage.getItem(ViewModeService.STORAGE_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
