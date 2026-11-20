import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import { ChangePasswordDTO, LoginDTO, RegisterDTO, UpdateProfileDTO, User } from '../../models/app-dto';
import { AuthResponse, MyProfile } from '../../models/auth-model';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private apiUrl = `${environment.apiUrl}/api/Auth`;

  constructor(private http: HttpClient) {}

  login(dto: LoginDTO): Observable<APIResponse<AuthResponse>> {
    return this.http.post<APIResponse<AuthResponse>>(`${this.apiUrl}/login`, dto);
  }

  register(dto: RegisterDTO): Observable<APIResponse<AuthResponse>> {
    return this.http.post<APIResponse<AuthResponse>>(`${this.apiUrl}/register`, dto);
  }

  changePassword(dto: ChangePasswordDTO): Observable<APIResponse<AuthResponse>> {
    return this.http.post<APIResponse<AuthResponse>>(`${this.apiUrl}/change-password`, dto);
  }

  me(): Observable<APIResponse<MyProfile>> {
    return this.http.get<APIResponse<MyProfile>>(`${this.apiUrl}/me`);
  }

  /** Full profile of the signed-in user (for the edit-profile modal). */
  getMyDetails(): Observable<APIResponse<User>> {
    return this.http.get<APIResponse<User>>(`${this.apiUrl}/me/details`);
  }

  updateMe(dto: UpdateProfileDTO): Observable<APIResponse<User>> {
    return this.http.put<APIResponse<User>>(`${this.apiUrl}/me`, dto);
  }

  adminExists(): Observable<APIResponse<{ exists: boolean }>> {
    return this.http.get<APIResponse<{ exists: boolean }>>(`${this.apiUrl}/admin-exists`);
  }
}
