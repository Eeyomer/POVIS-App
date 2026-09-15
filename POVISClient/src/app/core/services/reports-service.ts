import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, throwError } from 'rxjs';
import { OrdinanceSummaryReport, PerBarangayReport } from '../models/app-dto';
import { ReportsApi } from './api-service/reports-api';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  constructor(private api: ReportsApi) {}

  ordinanceSummary(from: string, to: string): Observable<OrdinanceSummaryReport> {
    return this.api.getOrdinanceSummary(from, to).pipe(
      map((res) => res.data as OrdinanceSummaryReport),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  semiAnnual(year: number, half: number): Observable<OrdinanceSummaryReport> {
    return this.api.getSemiAnnual(year, half).pipe(
      map((res) => res.data as OrdinanceSummaryReport),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  perBarangay(from: string, to: string): Observable<PerBarangayReport> {
    return this.api.getPerBarangay(from, to).pipe(
      map((res) => res.data as PerBarangayReport),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
