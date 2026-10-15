import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, from, map, throwError } from 'rxjs';
import { BulkImportResult } from '../models/app-dto';
import { BulkUploadApi } from './api-service/bulk-upload-api';

export type BulkUploadError = Error & { result?: BulkImportResult };

@Injectable({ providedIn: 'root' })
export class BulkUploadService {
  constructor(private api: BulkUploadApi) {}

  /** Downloads the module's .xlsx template (Instructions, data sheet, Example, Lists). */
  downloadTemplate(module: string): Observable<void> {
    return this.api.getTemplate(module).pipe(
      map((res) => this.save(res.body as Blob, `${module}-bulk-upload-template.xlsx`)),
      catchError((err: HttpErrorResponse) => from(this.blobError(err))),
    );
  }

  /** Resolves with the import summary. On validation errors the thrown error carries the per-row list in `result`. */
  upload(module: string, file: File): Observable<BulkImportResult> {
    return this.api.upload(module, file).pipe(
      map((res) => res.data as BulkImportResult),
      catchError((err: HttpErrorResponse) => {
        const error = new Error(err?.error?.message || 'Something went wrong. Please try again.') as BulkUploadError;
        error.result = err?.error?.data;
        return throwError(() => error);
      }),
    );
  }

  save(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }

  private async blobError(err: HttpErrorResponse): Promise<never> {
    let message = 'Could not download the template. Please try again.';
    try {
      if (err.error instanceof Blob) message = JSON.parse(await err.error.text())?.message || message;
    } catch {
      // keep the default message
    }
    throw new Error(message);
  }
}
