import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, throwError } from 'rxjs';
import { BackupModule, SystemHealth, TechnicalLogFilter } from '../models/app-dto';
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

  backupModules(): Observable<BackupModule[]> {
    return this.api.getBackupModules().pipe(
      map((res) => res.data ?? []),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  /** Restores one module; emits the number of records restored. */
  restore(moduleKey: string, file: File): Observable<number> {
    return this.api.restore(moduleKey, file).pipe(
      map((res) => res.data?.restored ?? 0),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  downloadBackup(moduleKey: string): Observable<void> {
    return this.api.downloadBackup(moduleKey).pipe(
      map((blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `povis-${moduleKey}-backup-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.json`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
      }),
      catchError(() => throwError(() => new Error('Could not download the backup. Please try again.'))),
    );
  }


  incentiveRate(): Observable<number> {
    return this.api.getSettings().pipe(
      map((res) => res.data?.incentiveRate ?? 10),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  setIncentiveRate(rate: number): Observable<number> {
    return this.api.setIncentiveRate(rate).pipe(
      map((res) => res.data?.incentiveRate ?? rate),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
