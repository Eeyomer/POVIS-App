import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { PagedResult } from '../models/app-model';
import { AddPaymentDTO, PaymentDTO, UpdatePaymentDTO } from '../models/app-dto';
import { PaymentsApi } from './api-service/payments-api';

const EMPTY_PAGE: PagedResult<PaymentDTO> = { items: [], totalCount: 0, page: 1, pageSize: 20 };

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  constructor(private api: PaymentsApi) {}

  list(from?: string | null, to?: string | null, page = 1, pageSize = 20, search?: string | null): Observable<PagedResult<PaymentDTO>> {
    return this.api.getAll(from, to, page, pageSize, search).pipe(
      map((res) => (res.success ? res.data ?? EMPTY_PAGE : EMPTY_PAGE)),
      catchError(() => of(EMPTY_PAGE)),
    );
  }

  getById(id: string): Observable<PaymentDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  getByViolationId(violationId: string): Observable<PaymentDTO | null> {
    return this.api.getByViolationId(violationId).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddPaymentDTO): Observable<PaymentDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as PaymentDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdatePaymentDTO): Observable<PaymentDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as PaymentDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  remove(id: string): Observable<void> {
    return this.api.delete(id).pipe(
      map(() => void 0),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
