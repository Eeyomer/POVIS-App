import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { PagedResult } from '../models/app-model';
import { AddViolatorDTO, UpdateViolatorDTO, ViolatorDTO, ViolatorHistoryDTO } from '../models/app-dto';
import { ViolatorsApi } from './api-service/violators-api';

const EMPTY_PAGE: PagedResult<ViolatorDTO> = { items: [], totalCount: 0, page: 1, pageSize: 20 };

@Injectable({ providedIn: 'root' })
export class ViolatorsService {
  constructor(private api: ViolatorsApi) {}

  search(search?: string, page = 1, pageSize = 20): Observable<PagedResult<ViolatorDTO>> {
    return this.api.search(search, page, pageSize).pipe(
      map((res) => (res.success ? res.data ?? EMPTY_PAGE : EMPTY_PAGE)),
      catchError(() => of(EMPTY_PAGE)),
    );
  }

  getById(id: string): Observable<ViolatorDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  getHistory(id: string): Observable<ViolatorHistoryDTO[]> {
    return this.api.getHistory(id).pipe(
      map((res) => (res.success ? res.data ?? [] : [])),
      catchError(() => of([])),
    );
  }

  create(dto: AddViolatorDTO): Observable<ViolatorDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as ViolatorDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateViolatorDTO): Observable<ViolatorDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as ViolatorDTO),
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
