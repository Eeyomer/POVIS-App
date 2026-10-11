import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import { CreateUserDTO, ResetPasswordResult, UpdateUserDTO, User } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private apiUrl = `${environment.apiUrl}/api/Users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<APIResponse<User[]>> {
    return this.http.get<APIResponse<User[]>>(this.apiUrl);
  }

  add(dto: CreateUserDTO): Observable<APIResponse<User>> {
    return this.http.post<APIResponse<User>>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateUserDTO): Observable<APIResponse<User>> {
    return this.http.put<APIResponse<User>>(`${this.apiUrl}/${id}`, dto);
  }

  resetPassword(id: string): Observable<APIResponse<ResetPasswordResult>> {
    return this.http.put<APIResponse<ResetPasswordResult>>(`${this.apiUrl}/${id}/reset-password`, {});
  }

  delete(id: string): Observable<APIResponse<null>> {
    return this.http.delete<APIResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
