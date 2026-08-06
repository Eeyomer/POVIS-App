import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import { AddFiledCaseDTO, CaseStatus, FiledCaseDTO, UpdateFiledCaseDTO } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class FiledCasesApi {
  private apiUrl = `${environment.apiUrl}/api/FiledCases`;

  constructor(private http: HttpClient) {}

  getAll(
    status?: CaseStatus | null,
    from?: string | null,
    to?: string | null,
    page = 1,
    pageSize = 20,
  ): Observable<APIResponse<PagedResult<FiledCaseDTO>>> {
    return this.http.get<APIResponse<PagedResult<FiledCaseDTO>>>(this.apiUrl, {
      params: {
        ...(status ? { status } : {}),
        ...(from ? { from } : {}),
        ...(to ? { to } : {}),
        page,
        pageSize,
      },
    });
  }

  getById(id: string): Observable<APIResponse<FiledCaseDTO>> {
    return this.http.get<APIResponse<FiledCaseDTO>>(`${this.apiUrl}/${id}`);
  }

  add(dto: AddFiledCaseDTO): Observable<APIResponse<FiledCaseDTO>> {
    return this.http.post<APIResponse<FiledCaseDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateFiledCaseDTO): Observable<APIResponse<FiledCaseDTO>> {
    return this.http.put<APIResponse<FiledCaseDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
