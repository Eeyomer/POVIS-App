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
 */
@Injectable({ providedIn: 'root' })
export class ViewModeService {
  private apiUrl = `${environment.apiUrl}/api/settings`;
  readonly modalForms = signal(false);

  constructor(private http: HttpClient) {}

  load(): void {
    this.http.get<APIResponse<AppSettings>>(this.apiUrl).subscribe({
      next: (res) => this.modalForms.set(!!res.data?.modalForms),
      error: () => {},
    });
  }

  setModalForms(enabled: boolean): Observable<APIResponse<AppSettings>> {
    return this.http
      .put<APIResponse<AppSettings>>(this.apiUrl, { modalForms: enabled })
      .pipe(tap((res) => this.modalForms.set(!!res.data?.modalForms)));
  }
}
