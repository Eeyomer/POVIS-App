import { Injectable, Inject, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { LoginDTO, RegisterDTO, Role, ROLE_LABELS } from '../models/app-dto';
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
      tap((auth) => this.storeSession(auth)),
      catchError((err) => this.rethrowAsError(err)),
    );
  }

  adminExists(): Observable<boolean> {
    return this.api.adminExists().pipe(
      map((res) => !!res.data?.exists),
      catchError(() => throwError(() => new Error('Unable to reach the server. Please try again.'))),
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

  getRoleLabel(): string {
    const role = this.getRole();
    return role ? ROLE_LABELS[role] : '';
  }

  hasRole(...roles: Role[]): boolean {
    const role = this.getRole();
    return !!role && roles.includes(role);
  }

  /** MIS Personnel: manages user accounts and roles, system maintenance. */
  canManageUsers(): boolean {
    return this.hasRole('MISPersonnel');
  }

  /** MIS Personnel and the OVR Supervisor may review/approve, update status, and generate reports. */
  canApprove(): boolean {
    return this.hasRole('MISPersonnel', 'OVRSupervisor');
  }

  /** Everyone except PNP/Barangay Staff (read-only verification) may create/edit records. */
  canEdit(): boolean {
    return this.hasRole('MISPersonnel', 'OVRSupervisor', 'ApprehendingOfficer');
  }

  /** Only MIS Personnel may permanently delete records. */
  canDelete(): boolean {
    return this.hasRole('MISPersonnel');
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
