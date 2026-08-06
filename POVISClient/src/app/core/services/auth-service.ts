import { Injectable, Inject, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { LoginDTO, RegisterDTO, Role } from '../models/app-dto';
import { AuthResponse } from '../models/auth-model';
import { AuthApi } from './api-service/auth-api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'povis_token';
  private readonly USERNAME_KEY = 'povis_username';
  private readonly ROLE_KEY = 'povis_role';
  private readonly isBrowser: boolean;

  private readonly _isLoggedIn = signal(false);
  readonly isLoggedIn$ = this._isLoggedIn.asReadonly();

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    private api: AuthApi,
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
    this._isLoggedIn.set(!!this.getToken());
  }

  login(credentials: LoginDTO): Observable<AuthResponse> {
    return this.api.login(credentials).pipe(
      map((res) => res.data as AuthResponse),
      tap((auth) => this.storeSession(auth)),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  register(dto: RegisterDTO): Observable<AuthResponse> {
    return this.api.register(dto).pipe(
      map((res) => res.data as AuthResponse),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  logout(): void {
    if (this.isBrowser) {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.USERNAME_KEY);
      localStorage.removeItem(this.ROLE_KEY);
    }
    this._isLoggedIn.set(false);
  }

  getToken(): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getUsername(): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem(this.USERNAME_KEY);
  }

  getRole(): Role | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem(this.ROLE_KEY) as Role | null;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  private storeSession(auth: AuthResponse): void {
    if (this.isBrowser) {
      localStorage.setItem(this.TOKEN_KEY, auth.token);
      localStorage.setItem(this.USERNAME_KEY, auth.username);
      localStorage.setItem(this.ROLE_KEY, auth.role);
    }
    this._isLoggedIn.set(true);
  }

  private rethrowAsError(err: HttpErrorResponse) {
    return throwError(() => new Error(err?.error?.message || 'Something went wrong. Please try again.'));
  }
}
