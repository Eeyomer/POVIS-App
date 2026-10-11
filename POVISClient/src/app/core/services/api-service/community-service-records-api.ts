import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import {
  AddCommunityServiceDTO,
  CommunityServiceDTO,
  ComplianceStatus,
  UpdateCommunityServiceDTO,
} from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class CommunityServiceRecordsApi {
  private apiUrl = `${environment.apiUrl}/api/CommunityServiceRecords`;

  constructor(private http: HttpClient) {}

  getAll(
    status?: ComplianceStatus | null,
    page = 1,
    pageSize = 20,
    search?: string | null,
  ): Observable<APIResponse<PagedResult<CommunityServiceDTO>>> {
    return this.http.get<APIResponse<PagedResult<CommunityServiceDTO>>>(this.apiUrl, {
      params: { ...(status ? { status } : {}), page, pageSize, ...(search ? { search } : {}) },
    });
  }

  getById(id: string): Observable<APIResponse<CommunityServiceDTO>> {
    return this.http.get<APIResponse<CommunityServiceDTO>>(`${this.apiUrl}/${id}`);
  }

  add(dto: AddCommunityServiceDTO): Observable<APIResponse<CommunityServiceDTO>> {
    return this.http.post<APIResponse<CommunityServiceDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCommunityServiceDTO): Observable<APIResponse<CommunityServiceDTO>> {
    return this.http.put<APIResponse<CommunityServiceDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
