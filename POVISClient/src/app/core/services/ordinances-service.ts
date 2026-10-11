import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
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
} from '../models/app-dto';
import { OrdinancesApi } from './api-service/ordinances-api';

@Injectable({ providedIn: 'root' })
export class OrdinancesService {
  constructor(private api: OrdinancesApi) {}

  list(includeInactive = false): Observable<OrdinanceListDTO[]> {
    return this.api.getAll(includeInactive).pipe(
      map((res) => (res.success ? res.data ?? [] : [])),
      catchError(() => of([])),
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
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateOrdinanceDTO): Observable<OrdinanceDTO> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as OrdinanceDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  remove(id: string): Observable<void> {
    return this.api.delete(id).pipe(
      map(() => void 0),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  addSection(id: string, dto: AddOrdinanceSectionDTO): Observable<OrdinanceSectionDTO> {
    return this.api.addSection(id, dto).pipe(
      map((res) => res.data as OrdinanceSectionDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  removeSection(id: string, sectionId: string): Observable<void> {
    return this.api.removeSection(id, sectionId).pipe(
      map(() => void 0),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  addPenalty(id: string, dto: AddOrdinancePenaltyDTO): Observable<OrdinancePenaltyDTO> {
    return this.api.addPenalty(id, dto).pipe(
      map((res) => res.data as OrdinancePenaltyDTO),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  removePenalty(id: string, penaltyId: string): Observable<void> {
    return this.api.removePenalty(id, penaltyId).pipe(
      map(() => void 0),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  /** Resolves with the import summary. On validation errors the thrown error carries the per-row list in `result`. */
  bulkUpload(file: File): Observable<OrdinanceImportResultDTO> {
    return this.api.bulkUpload(file).pipe(
      map((res) => res.data as OrdinanceImportResultDTO),
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
