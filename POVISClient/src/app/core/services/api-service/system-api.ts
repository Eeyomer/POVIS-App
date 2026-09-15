import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import { SystemHealth, TechnicalLog, TechnicalLogFilter } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class SystemApi {
  private apiUrl = `${environment.apiUrl}/api/system`;

  constructor(private http: HttpClient) {}

  getHealth(): Observable<APIResponse<SystemHealth>> {
    return this.http.get<APIResponse<SystemHealth>>(`${this.apiUrl}/health`);
  }

  downloadBackup(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/backup`, { responseType: 'blob' });
  }

  restore(file: File): Observable<APIResponse<null>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<APIResponse<null>>(`${this.apiUrl}/restore`, formData);
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
}
