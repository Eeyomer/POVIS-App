import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, from, map, throwError } from 'rxjs';
import { OrdinanceSummaryReport, PerBarangayReport, ReportExportFormat, ReportExportType } from '../models/app-dto';
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

  /** Fetches the PDF/Excel file and hands it to the browser as a download. */
  download(report: ReportExportType, format: ReportExportFormat, params: Record<string, string>): Observable<void> {
    return this.api.exportReport(report, format, params).pipe(
      map((res) => {
        const disposition = res.headers.get('content-disposition') ?? '';
        const match = /filename*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
        const fileName = match ? decodeURIComponent(match[1]) : `povis-${report}.${format}`;
        const url = URL.createObjectURL(res.body as Blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
      }),
      catchError((err: HttpErrorResponse) => from(this.blobError(err))),
    );
  }

  private async blobError(err: HttpErrorResponse): Promise<never> {
    let message = 'Could not generate the report. Please try again.';
    try {
      if (err.error instanceof Blob) message = JSON.parse(await err.error.text())?.message || message;
    } catch {
      // keep the default message
    }
    throw new Error(message);
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
