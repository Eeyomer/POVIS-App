import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { AddBarangayDTO, BarangayDTO, UpdateBarangayDTO } from '../models/app-dto';
import { BarangaysApi } from './api-service/barangays-api';

@Injectable({ providedIn: 'root' })
export class BarangaysService {
  constructor(private api: BarangaysApi) {}

  list(includeInactive = false): Observable<BarangayDTO[]> {
    return this.api.getAll(includeInactive).pipe(
      map((res) => (res.success ? res.data ?? [] : [])),
      catchError(() => of([])),
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
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateBarangayDTO): Observable<BarangayDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as BarangayDTO),
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
