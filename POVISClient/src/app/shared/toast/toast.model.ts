export type ToastSeverity = 'success' | 'info' | 'warn' | 'error';

export interface ToastMessage {
  id: string;
  severity: ToastSeverity;
  summary?: string;
  detail: string;
  /** Auto-dismiss delay in ms. Set to 0 to make it sticky (stays until closed). */
  life: number;
  closable: boolean;
}

export interface ToastOptions {
  summary?: string;
  life?: number;
  closable?: boolean;
}
