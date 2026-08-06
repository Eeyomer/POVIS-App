import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { PagedResult } from '../models/app-model';
import {
  AddCommunityServiceDTO,
  CommunityServiceDTO,
  ComplianceStatus,
  UpdateCommunityServiceDTO,
} from '../models/app-dto';
import { CommunityServiceRecordsApi } from './api-service/community-service-records-api';

const EMPTY_PAGE: PagedResult<CommunityServiceDTO> = { items: [], totalCount: 0, page: 1, pageSize: 20 };

@Injectable({ providedIn: 'root' })
export class CommunityServiceRecordsService {
  constructor(private api: CommunityServiceRecordsApi) {}

  list(status?: ComplianceStatus | null, page = 1, pageSize = 20): Observable<PagedResult<CommunityServiceDTO>> {
    return this.api.getAll(status, page, pageSize).pipe(
      map((res) => (res.success ? res.data ?? EMPTY_PAGE : EMPTY_PAGE)),
      catchError(() => of(EMPTY_PAGE)),
    );
  }

  getById(id: string): Observable<CommunityServiceDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddCommunityServiceDTO): Observable<CommunityServiceDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as CommunityServiceDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateCommunityServiceDTO): Observable<CommunityServiceDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as CommunityServiceDTO),
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
