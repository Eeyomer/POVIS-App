import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, shareReplay, tap, throwError } from 'rxjs';
import {
  AddOrdinanceDTO,
  AddOrdinancePenaltyDTO,
  AddOrdinanceSectionDTO,
  OrdinanceDTO,
  OrdinanceImportResultDTO,
  OrdinanceListDTO,
  OrdinancePenaltyDTO,
  OrdinanceSectionDTO,
  UpdateOrdinanceDTO,
  UpdateOrdinancePenaltyDTO,
} from '../models/app-dto';
import { OrdinancesApi } from './api-service/ordinances-api';
import { TtlCache } from '../utils/ttl-cache';

@Injectable({ providedIn: 'root' })
export class OrdinancesService {
  constructor(private api: OrdinancesApi) {}

  /** Ordinances rarely change but are loaded by many pages: kept until the page reloads or one is changed. */
  private cache = new TtlCache<Observable<OrdinanceListDTO[]>>();

  /** Drops the cached lists (also called after a bulk upload of ordinances). */
  invalidate(): void {
    this.cache.clear();
  }

  list(includeInactive = false): Observable<OrdinanceListDTO[]> {
    const key = String(includeInactive);
    let source = this.cache.get(key);
    if (!source) {
      source = this.api.getAll(includeInactive).pipe(
        map((res) => {
          if (!res.success) throw new Error(res.message);
          return res.data ?? [];
        }),
        tap({ error: () => this.cache.delete(key) }), // never keep a failed load
        shareReplay({ bufferSize: 1, refCount: false }),
      );
      this.cache.set(key, source);
    }
    return source.pipe(
      map((list) => [...list]),
      catchError(() => of([] as OrdinanceListDTO[])),
    );
  }

  getById(id: string): Observable<OrdinanceDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddOrdinanceDTO): Observable<OrdinanceDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as OrdinanceDTO),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateOrdinanceDTO): Observable<OrdinanceDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as OrdinanceDTO),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  remove(id: string): Observable<void> {
    return this.api.delete(id).pipe(
      map(() => void 0),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  addSection(id: string, dto: AddOrdinanceSectionDTO): Observable<OrdinanceSectionDTO> {
    return this.api.addSection(id, dto).pipe(
      map((res) => res.data as OrdinanceSectionDTO),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  updateSection(id: string, sectionId: string, dto: AddOrdinanceSectionDTO): Observable<OrdinanceSectionDTO> {
    return this.api.updateSection(id, sectionId, dto).pipe(
      map((res) => res.data as OrdinanceSectionDTO),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  updatePenalty(id: string, penaltyId: string, dto: UpdateOrdinancePenaltyDTO): Observable<OrdinancePenaltyDTO> {
    return this.api.updatePenalty(id, penaltyId, dto).pipe(
      map((res) => res.data as OrdinancePenaltyDTO),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  removeSection(id: string, sectionId: string): Observable<void> {
    return this.api.removeSection(id, sectionId).pipe(
      map(() => void 0),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  addPenalty(id: string, dto: AddOrdinancePenaltyDTO): Observable<OrdinancePenaltyDTO> {
    return this.api.addPenalty(id, dto).pipe(
      map((res) => res.data as OrdinancePenaltyDTO),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  removePenalty(id: string, penaltyId: string): Observable<void> {
    return this.api.removePenalty(id, penaltyId).pipe(
      map(() => void 0),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  /** Resolves with the import summary. On validation errors the thrown error carries the per-row list in `result`. */
  bulkUpload(file: File): Observable<OrdinanceImportResultDTO> {
    return this.api.bulkUpload(file).pipe(
      map((res) => res.data as OrdinanceImportResultDTO),
      tap(() => this.invalidate()),
      catchError((err: HttpErrorResponse) => {
        const error = new Error(err?.error?.message || 'Something went wrong. Please try again.') as Error & {
          result?: OrdinanceImportResultDTO;
        };
        error.result = err?.error?.data;
        return throwError(() => error);
      }),
    );
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
