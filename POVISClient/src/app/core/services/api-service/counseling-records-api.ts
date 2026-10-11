import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import { AddCounselingDTO, ComplianceStatus, CounselingDTO, UpdateCounselingDTO } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class CounselingRecordsApi {
  private apiUrl = `${environment.apiUrl}/api/CounselingRecords`;

  constructor(private http: HttpClient) {}

  getAll(
    status?: ComplianceStatus | null,
    page = 1,
    pageSize = 20,
    search?: string | null,
  ): Observable<APIResponse<PagedResult<CounselingDTO>>> {
    return this.http.get<APIResponse<PagedResult<CounselingDTO>>>(this.apiUrl, {
      params: { ...(status ? { status } : {}), page, pageSize, ...(search ? { search } : {}) },
    });
  }

  getById(id: string): Observable<APIResponse<CounselingDTO>> {
    return this.http.get<APIResponse<CounselingDTO>>(`${this.apiUrl}/${id}`);
  }

  add(dto: AddCounselingDTO): Observable<APIResponse<CounselingDTO>> {
    return this.http.post<APIResponse<CounselingDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCounselingDTO): Observable<APIResponse<CounselingDTO>> {
    return this.http.put<APIResponse<CounselingDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
