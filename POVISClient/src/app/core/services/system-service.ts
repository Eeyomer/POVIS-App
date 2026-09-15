import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, throwError } from 'rxjs';
import { SystemHealth, TechnicalLogFilter } from '../models/app-dto';
import { PagedResult } from '../models/app-model';
import { TechnicalLog } from '../models/app-dto';
import { SystemApi } from './api-service/system-api';

@Injectable({ providedIn: 'root' })
export class SystemService {
  constructor(private api: SystemApi) {}

  health(): Observable<SystemHealth> {
    return this.api.getHealth().pipe(
      map((res) => res.data as SystemHealth),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  logs(filter: TechnicalLogFilter): Observable<PagedResult<TechnicalLog>> {
    return this.api.getLogs(filter).pipe(
      map((res) => res.data as PagedResult<TechnicalLog>),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  restore(file: File): Observable<void> {
    return this.api.restore(file).pipe(
      map(() => void 0),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  downloadBackup(): void {
    this.api.downloadBackup().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `povis-backup-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.json`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
      },
    });
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
