import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import {
  AddViolationDTO,
  ApplicablePenaltyDTO,
  UpdateViolationDTO,
  ViolationDTO,
  ViolationFilterDTO,
} from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class ViolationsApi {
  private apiUrl = `${environment.apiUrl}/api/Violations`;

  constructor(private http: HttpClient) {}

  getAll(filter: Partial<ViolationFilterDTO>): Observable<APIResponse<PagedResult<ViolationDTO>>> {
    return this.http.get<APIResponse<PagedResult<ViolationDTO>>>(this.apiUrl, { params: this.toParams(filter) });
  }

  /** Drops unset filters; Angular would otherwise send undefined as the string "undefined". */
  private toParams(filter: Partial<ViolationFilterDTO>): Record<string, string | number | boolean> {
    return Object.fromEntries(
      Object.entries(filter).filter(([, v]) => v !== undefined && v !== null && v !== ""),
    ) as Record<string, string | number | boolean>;
  }

  getById(id: string): Observable<APIResponse<ViolationDTO>> {
    return this.http.get<APIResponse<ViolationDTO>>(`${this.apiUrl}/${id}`);
  }

  getApplicablePenalty(id: string): Observable<APIResponse<ApplicablePenaltyDTO>> {
    return this.http.get<APIResponse<ApplicablePenaltyDTO>>(`${this.apiUrl}/${id}/penalty`);
  }

  add(dto: AddViolationDTO): Observable<APIResponse<ViolationDTO>> {
    return this.http.post<APIResponse<ViolationDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateViolationDTO): Observable<APIResponse<ViolationDTO>> {
    return this.http.put<APIResponse<ViolationDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  setReviewed(id: string, reviewed: boolean): Observable<APIResponse<ViolationDTO>> {
    return this.http.put<APIResponse<ViolationDTO>>(`${this.apiUrl}/${id}/review`, { reviewed });
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
