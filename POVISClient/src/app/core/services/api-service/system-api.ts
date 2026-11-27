import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import { BackupModule, SystemHealth, TechnicalLog, TechnicalLogFilter } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class SystemApi {
  private apiUrl = `${environment.apiUrl}/api/system`;

  constructor(private http: HttpClient) {}

  getHealth(): Observable<APIResponse<SystemHealth>> {
    return this.http.get<APIResponse<SystemHealth>>(`${this.apiUrl}/health`);
  }

  getBackupModules(): Observable<APIResponse<BackupModule[]>> {
    return this.http.get<APIResponse<BackupModule[]>>(`${this.apiUrl}/backup/modules`);
  }

  downloadBackup(moduleKey: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/backup/${encodeURIComponent(moduleKey)}`, { responseType: 'blob' });
  }

  restore(moduleKey: string, file: File): Observable<APIResponse<{ restored: number }>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<APIResponse<{ restored: number }>>(
      `${this.apiUrl}/restore/${encodeURIComponent(moduleKey)}`,
      formData,
    );
  }

  getLogs(filter: TechnicalLogFilter): Observable<APIResponse<PagedResult<TechnicalLog>>> {
    let params = new HttpParams();
    if (filter.search) params = params.set('search', filter.search);
    if (filter.method) params = params.set('method', filter.method);
    if (filter.statusCode) params = params.set('statusCode', filter.statusCode);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    params = params.set('page', filter.page ?? 1);
    params = params.set('pageSize', filter.pageSize ?? 50);

    return this.http.get<APIResponse<PagedResult<TechnicalLog>>>(`${this.apiUrl}/logs`, { params });
  }

  getSettings(): Observable<APIResponse<{ modalForms: boolean; incentiveRate: number }>> {
    return this.http.get<APIResponse<{ modalForms: boolean; incentiveRate: number }>>(`${environment.apiUrl}/api/settings`);
  }

  /** New rate applies only to payments recorded from now on; existing payments keep theirs. */
  setIncentiveRate(rate: number): Observable<APIResponse<{ modalForms: boolean; incentiveRate: number }>> {
    return this.http.put<APIResponse<{ modalForms: boolean; incentiveRate: number }>>(
      `${environment.apiUrl}/api/settings/incentive-rate`,
      { rate },
    );
  }
}
