import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import { AddViolatorDTO, UpdateViolatorDTO, ViolatorDTO, ViolatorHistoryDTO } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class ViolatorsApi {
  private apiUrl = `${environment.apiUrl}/api/Violators`;

  constructor(private http: HttpClient) {}

  search(search?: string, page = 1, pageSize = 20): Observable<APIResponse<PagedResult<ViolatorDTO>>> {
    return this.http.get<APIResponse<PagedResult<ViolatorDTO>>>(this.apiUrl, {
      params: { ...(search ? { search } : {}), page, pageSize },
    });
  }

  getById(id: string): Observable<APIResponse<ViolatorDTO>> {
    return this.http.get<APIResponse<ViolatorDTO>>(`${this.apiUrl}/${id}`);
  }

  getHistory(id: string): Observable<APIResponse<ViolatorHistoryDTO[]>> {
    return this.http.get<APIResponse<ViolatorHistoryDTO[]>>(`${this.apiUrl}/${id}/history`);
  }

  add(dto: AddViolatorDTO): Observable<APIResponse<ViolatorDTO>> {
    return this.http.post<APIResponse<ViolatorDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateViolatorDTO): Observable<APIResponse<ViolatorDTO>> {
    return this.http.put<APIResponse<ViolatorDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
