import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { CreateUserDTO, UpdateUserDTO, User } from '../models/app-dto';
import { UsersApi } from './api-service/users-api';

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private api: UsersApi) {}

  list(): Observable<User[]> {
    return this.api.getAll().pipe(
      map((res) => (res.success ? res.data ?? [] : [])),
      catchError(() => of([])),
    );
  }

  create(dto: CreateUserDTO): Observable<User> {
    return this.api.add(dto).pipe(
      map((res) => res.data as User),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  update(id: string, dto: UpdateUserDTO): Observable<User> {
    return this.api.update(id, dto).pipe(
      map((res) => res.data as User),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  /** Resolves with the new system-generated temporary password. */
  resetPassword(id: string): Observable<string> {
    return this.api.resetPassword(id).pipe(
      map((res) => res.data?.temporaryPassword as string),
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
