import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import { BulkImportResult } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class BulkUploadApi {
  private apiUrl = `${environment.apiUrl}/api/BulkUpload`;

  constructor(private http: HttpClient) {}

  getTemplate(module: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.apiUrl}/${module}/template`, { responseType: 'blob', observe: 'response' });
  }

  upload(module: string, file: File): Observable<APIResponse<BulkImportResult>> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<APIResponse<BulkImportResult>>(`${this.apiUrl}/${module}`, body);
  }
}
