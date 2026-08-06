import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import {
  AddSeminarRecordDTO,
  ComplianceStatus,
  SeminarRecordDTO,
  SeminarType,
  UpdateSeminarRecordDTO,
} from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class SeminarRecordsApi {
  private apiUrl = `${environment.apiUrl}/api/SeminarRecords`;

  constructor(private http: HttpClient) {}

  getAll(
    type?: SeminarType | null,
    status?: ComplianceStatus | null,
    page = 1,
    pageSize = 20,
  ): Observable<APIResponse<PagedResult<SeminarRecordDTO>>> {
    return this.http.get<APIResponse<PagedResult<SeminarRecordDTO>>>(this.apiUrl, {
      params: { ...(type ? { type } : {}), ...(status ? { status } : {}), page, pageSize },
    });
  }

  getById(id: string): Observable<APIResponse<SeminarRecordDTO>> {
    return this.http.get<APIResponse<SeminarRecordDTO>>(`${this.apiUrl}/${id}`);
  }

  add(dto: AddSeminarRecordDTO): Observable<APIResponse<SeminarRecordDTO>> {
    return this.http.post<APIResponse<SeminarRecordDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateSeminarRecordDTO): Observable<APIResponse<SeminarRecordDTO>> {
    return this.http.put<APIResponse<SeminarRecordDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
