import { Role } from './app-dto';

export interface AuthResponse {
  token: string;
  username: string;
  role: Role;
  expiresAt: string;
}
