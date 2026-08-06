import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import { LoginDTO, RegisterDTO } from '../../models/app-dto';
import { AuthResponse } from '../../models/auth-model';

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

  adminExists(): Observable<APIResponse<{ exists: boolean }>> {
    return this.http.get<APIResponse<{ exists: boolean }>>(`${this.apiUrl}/admin-exists`);
  }
}
