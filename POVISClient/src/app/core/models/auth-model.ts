import { Role } from './app-dto';

export interface AuthResponse {
  token: string;
  username: string;
  role: Role;
  expiresAt: string;
  mustChangePassword: boolean;
}

/** The signed-in user's own display profile. */
export interface MyProfile {
  username: string;
  fullName?: string | null;
  /** Base64 data URL, or null when no photo was uploaded. */
  photo?: string | null;
}
