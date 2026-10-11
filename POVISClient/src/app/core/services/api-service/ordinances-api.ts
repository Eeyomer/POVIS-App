import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import {
  AddOrdinanceDTO,
  AddOrdinancePenaltyDTO,
  AddOrdinanceSectionDTO,
  OrdinanceDTO,
  OrdinanceImportResultDTO,
  OrdinanceListDTO,
  OrdinancePenaltyDTO,
  OrdinanceSectionDTO,
  UpdateOrdinanceDTO,
} from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class OrdinancesApi {
  private apiUrl = `${environment.apiUrl}/api/Ordinances`;

  constructor(private http: HttpClient) {}

  getAll(includeInactive = false): Observable<APIResponse<OrdinanceListDTO[]>> {
    return this.http.get<APIResponse<OrdinanceListDTO[]>>(this.apiUrl, { params: { includeInactive } });
  }

  getById(id: string): Observable<APIResponse<OrdinanceDTO>> {
    return this.http.get<APIResponse<OrdinanceDTO>>(`${this.apiUrl}/${id}`);
  }

  add(dto: AddOrdinanceDTO): Observable<APIResponse<OrdinanceDTO>> {
    return this.http.post<APIResponse<OrdinanceDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateOrdinanceDTO): Observable<APIResponse<OrdinanceDTO>> {
    return this.http.put<APIResponse<OrdinanceDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }

  addSection(id: string, dto: AddOrdinanceSectionDTO): Observable<APIResponse<OrdinanceSectionDTO>> {
    return this.http.post<APIResponse<OrdinanceSectionDTO>>(`${this.apiUrl}/${id}/sections`, dto);
  }

  removeSection(id: string, sectionId: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}/sections/${sectionId}`);
  }

  addPenalty(id: string, dto: AddOrdinancePenaltyDTO): Observable<APIResponse<OrdinancePenaltyDTO>> {
    return this.http.post<APIResponse<OrdinancePenaltyDTO>>(`${this.apiUrl}/${id}/penalties`, dto);
  }

  bulkUpload(file: File): Observable<APIResponse<OrdinanceImportResultDTO>> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<APIResponse<OrdinanceImportResultDTO>>(`${this.apiUrl}/bulk-upload`, body);
  }

  removePenalty(id: string, penaltyId: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}/penalties/${penaltyId}`);
  }
}
