import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import { OrdinanceSummaryReport, PerBarangayReport } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private apiUrl = `${environment.apiUrl}/api/Reports`;

  constructor(private http: HttpClient) {}

  getOrdinanceSummary(from: string, to: string): Observable<APIResponse<OrdinanceSummaryReport>> {
    return this.http.get<APIResponse<OrdinanceSummaryReport>>(`${this.apiUrl}/ordinance-summary`, {
      params: { from, to },
    });
  }

  getSemiAnnual(year: number, half: number): Observable<APIResponse<OrdinanceSummaryReport>> {
    return this.http.get<APIResponse<OrdinanceSummaryReport>>(`${this.apiUrl}/semi-annual/${year}/${half}`);
  }

  getPerBarangay(from: string, to: string): Observable<APIResponse<PerBarangayReport>> {
    return this.http.get<APIResponse<PerBarangayReport>>(`${this.apiUrl}/per-barangay`, {
      params: { from, to },
    });
  }
}
