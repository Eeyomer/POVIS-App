import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { PagedResult } from '../models/app-model';
import { AddFiledCaseDTO, CaseStatus, FiledCaseDTO, UpdateFiledCaseDTO } from '../models/app-dto';
import { FiledCasesApi } from './api-service/filed-cases-api';

const EMPTY_PAGE: PagedResult<FiledCaseDTO> = { items: [], totalCount: 0, page: 1, pageSize: 20 };

@Injectable({ providedIn: 'root' })
export class FiledCasesService {
  constructor(private api: FiledCasesApi) {}

  list(
    status?: CaseStatus | null,
    from?: string | null,
    to?: string | null,
    page = 1,
    pageSize = 20,
  ): Observable<PagedResult<FiledCaseDTO>> {
    return this.api.getAll(status, from, to, page, pageSize).pipe(
      map((res) => (res.success ? res.data ?? EMPTY_PAGE : EMPTY_PAGE)),
      catchError(() => of(EMPTY_PAGE)),
    );
  }

  getById(id: string): Observable<FiledCaseDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddFiledCaseDTO): Observable<FiledCaseDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as FiledCaseDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateFiledCaseDTO): Observable<FiledCaseDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as FiledCaseDTO),
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
