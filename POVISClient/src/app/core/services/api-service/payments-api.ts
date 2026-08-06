import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse, PagedResult } from '../../models/app-model';
import { AddPaymentDTO, PaymentDTO, UpdatePaymentDTO } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class PaymentsApi {
  private apiUrl = `${environment.apiUrl}/api/Payments`;

  constructor(private http: HttpClient) {}

  getAll(
    from?: string | null,
    to?: string | null,
    page = 1,
    pageSize = 20,
  ): Observable<APIResponse<PagedResult<PaymentDTO>>> {
    return this.http.get<APIResponse<PagedResult<PaymentDTO>>>(this.apiUrl, {
      params: { ...(from ? { from } : {}), ...(to ? { to } : {}), page, pageSize },
    });
  }

  getById(id: string): Observable<APIResponse<PaymentDTO>> {
    return this.http.get<APIResponse<PaymentDTO>>(`${this.apiUrl}/${id}`);
  }

  getByViolationId(violationId: string): Observable<APIResponse<PaymentDTO>> {
    return this.http.get<APIResponse<PaymentDTO>>(`${this.apiUrl}/by-violation/${violationId}`);
  }

  add(dto: AddPaymentDTO): Observable<APIResponse<PaymentDTO>> {
    return this.http.post<APIResponse<PaymentDTO>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdatePaymentDTO): Observable<APIResponse<PaymentDTO>> {
    return this.http.put<APIResponse<PaymentDTO>>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
