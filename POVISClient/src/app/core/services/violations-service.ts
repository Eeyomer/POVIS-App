import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, shareReplay, tap, throwError } from 'rxjs';
import { PagedResult } from '../models/app-model';
import {
  AddViolationDTO,
  ApplicablePenaltyDTO,
  OfficerOption,
  UpdateViolationDTO,
  ViolationDTO,
  ViolationFilterDTO,
} from '../models/app-dto';
import { ViolationsApi } from './api-service/violations-api';
import { TtlCache } from '../utils/ttl-cache';

const EMPTY_PAGE: PagedResult<ViolationDTO> = { items: [], totalCount: 0, page: 1, pageSize: 20 };

@Injectable({ providedIn: 'root' })
export class ViolationsService {
  constructor(private api: ViolationsApi) {}

  list(filter: Partial<ViolationFilterDTO> = {}): Observable<PagedResult<ViolationDTO>> {
    const merged: Partial<ViolationFilterDTO> = { page: 1, pageSize: 20, ...filter };
    return this.api.getAll(merged).pipe(
      map((res) => (res.success ? res.data ?? EMPTY_PAGE : EMPTY_PAGE)),
      catchError(() => of(EMPTY_PAGE)),
    );
  }

  getById(id: string): Observable<ViolationDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  /** Registered officers change when accounts are added/edited: kept for 5 minutes, or until an account changes. */
  private officersCache = new TtlCache<Observable<OfficerOption[]>>(5 * 60 * 1000);

  /** Drops the cached officer list (called when a user account is created, edited or imported). */
  invalidateOfficers(): void {
    this.officersCache.clear();
  }

  getOfficers(): Observable<OfficerOption[]> {
    let source = this.officersCache.get('officers');
    if (!source) {
      source = this.api.getOfficers().pipe(
        map((res) => {
          if (!res.success) throw new Error(res.message);
          return res.data ?? [];
        }),
        tap({ error: () => this.officersCache.delete('officers') }), // never keep a failed load
        shareReplay({ bufferSize: 1, refCount: false }),
      );
      this.officersCache.set('officers', source);
    }
    return source.pipe(
      map((list) => [...list]),
      catchError(() => of([] as OfficerOption[])),
    );
  }

  getApplicablePenalty(id: string): Observable<ApplicablePenaltyDTO | null> {
    return this.api.getApplicablePenalty(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddViolationDTO): Observable<ViolationDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as ViolationDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateViolationDTO): Observable<ViolationDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as ViolationDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  remove(id: string): Observable<void> {
    return this.api.delete(id).pipe(
      map(() => void 0),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  setReviewed(id: string, reviewed: boolean): Observable<ViolationDTO> {
    return this.api.setReviewed(id, reviewed).pipe(
      map((res) => res.data as ViolationDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
