import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { PagedResult } from '../models/app-model';
import {
  AddSeminarRecordDTO,
  ComplianceStatus,
  SeminarRecordDTO,
  SeminarType,
  UpdateSeminarRecordDTO,
} from '../models/app-dto';
import { SeminarRecordsApi } from './api-service/seminar-records-api';

const EMPTY_PAGE: PagedResult<SeminarRecordDTO> = { items: [], totalCount: 0, page: 1, pageSize: 20 };

@Injectable({ providedIn: 'root' })
export class SeminarRecordsService {
  constructor(private api: SeminarRecordsApi) {}

  list(
    type?: SeminarType | null,
    status?: ComplianceStatus | null,
    page = 1,
    pageSize = 20,
  ): Observable<PagedResult<SeminarRecordDTO>> {
    return this.api.getAll(type, status, page, pageSize).pipe(
      map((res) => (res.success ? res.data ?? EMPTY_PAGE : EMPTY_PAGE)),
      catchError(() => of(EMPTY_PAGE)),
    );
  }

  getById(id: string): Observable<SeminarRecordDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddSeminarRecordDTO): Observable<SeminarRecordDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as SeminarRecordDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateSeminarRecordDTO): Observable<SeminarRecordDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as SeminarRecordDTO),
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
