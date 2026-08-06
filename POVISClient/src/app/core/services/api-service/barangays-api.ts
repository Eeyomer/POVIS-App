import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import { AddBarangayDTO, BarangayDTO, UpdateBarangayDTO } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class BarangaysApi {
  private apiUrl = `${environment.apiUrl}/api/Barangays`;

  constructor(private http: HttpClient) {}

  getAll(includeInactive = false): Observable<APIResponse<BarangayDTO[]>> {
    return this.http.get<APIResponse<BarangayDTO[]>>(this.apiUrl, { params: { includeInactive } });
  }

  getById(id: string): Observable<APIResponse<BarangayDTO>> {
    return this.http.get<APIResponse<BarangayDTO>>(`${this.apiUrl}/${id}`);
  }

  add(dto: AddBarangayDTO): Observable<APIResponse<BarangayDTO>> {
    return this.http.post<APIResponse<BarangayDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateBarangayDTO): Observable<APIResponse<BarangayDTO>> {
    return this.http.put<APIResponse<BarangayDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
