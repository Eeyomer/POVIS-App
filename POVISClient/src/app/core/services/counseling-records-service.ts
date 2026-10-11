import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { PagedResult } from '../models/app-model';
import { AddCounselingDTO, ComplianceStatus, CounselingDTO, UpdateCounselingDTO } from '../models/app-dto';
import { CounselingRecordsApi } from './api-service/counseling-records-api';

const EMPTY_PAGE: PagedResult<CounselingDTO> = { items: [], totalCount: 0, page: 1, pageSize: 20 };

@Injectable({ providedIn: 'root' })
export class CounselingRecordsService {
  constructor(private api: CounselingRecordsApi) {}

  list(status?: ComplianceStatus | null, page = 1, pageSize = 20, search?: string | null): Observable<PagedResult<CounselingDTO>> {
    return this.api.getAll(status, page, pageSize, search).pipe(
      map((res) => (res.success ? res.data ?? EMPTY_PAGE : EMPTY_PAGE)),
      catchError(() => of(EMPTY_PAGE)),
    );
  }

  getById(id: string): Observable<CounselingDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddCounselingDTO): Observable<CounselingDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as CounselingDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateCounselingDTO): Observable<CounselingDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as CounselingDTO),
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
