import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth-service';
import { StorageNoticeService, STORAGE_FULL_MESSAGE } from '../services/storage-notice-service';
import { isStorageFull } from '../environment/availability';

const AUTH_ENDPOINTS = ['/api/Auth/login', '/api/Auth/register', '/api/Auth/admin-exists'];

/** POSTs that don't add records, so they stay allowed while storage is full. */
const STORAGE_EXEMPT_POSTS = ['/api/Auth/login', '/api/Auth/change-password'];

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Storage is full (IS_STORAGE_FULL): every add is POST, while edit and delete use PUT/PATCH/DELETE and stay allowed.
  if (isStorageFull && req.method === 'POST' && !STORAGE_EXEMPT_POSTS.some((path) => req.url.includes(path))) {
    inject(StorageNoticeService).show();
    return throwError(
      () => new HttpErrorResponse({ status: 507, url: req.url, error: { success: false, message: STORAGE_FULL_MESSAGE } }),
    );
  }

  const token = authService.getToken();
  const isAuthEndpoint = AUTH_ENDPOINTS.some((path) => req.url.includes(path));

  const authReq = token && !isAuthEndpoint
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login']);
      }
      if (error.status === 403 && error.error?.errorCode === 'PasswordChangeRequired') {
        authService.flagMustChangePassword();
        router.navigate(['/change-password']);
      }
      return throwError(() => error);
    }),
  );
};
