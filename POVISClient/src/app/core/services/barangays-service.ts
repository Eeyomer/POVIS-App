import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, shareReplay, tap, throwError } from 'rxjs';
import { AddBarangayDTO, BarangayDTO, UpdateBarangayDTO } from '../models/app-dto';
import { BarangaysApi } from './api-service/barangays-api';
import { TtlCache } from '../utils/ttl-cache';

@Injectable({ providedIn: 'root' })
export class BarangaysService {
  constructor(private api: BarangaysApi) {}

  /** Barangays rarely change but are loaded by many pages: kept until the page reloads or one is added/edited/removed. */
  private cache = new TtlCache<Observable<BarangayDTO[]>>();

  /** Drops the cached lists (also called after a bulk upload of barangays). */
  invalidate(): void {
    this.cache.clear();
  }

  list(includeInactive = false): Observable<BarangayDTO[]> {
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
      catchError(() => of([] as BarangayDTO[])),
    );
  }

  getById(id: string): Observable<BarangayDTO | null> {
    return this.api.getById(id).pipe(
      map((res) => res.data),
      catchError(() => of(null)),
    );
  }

  create(dto: AddBarangayDTO): Observable<BarangayDTO> {
    return this.api.add(dto).pipe(
      map((res) => res.data as BarangayDTO),
      tap(() => this.invalidate()),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateBarangayDTO): Observable<BarangayDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as BarangayDTO),
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

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
