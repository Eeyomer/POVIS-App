import { Injectable, signal } from '@angular/core';
import { ToastMessage, ToastOptions, ToastSeverity } from './toast.model';

const DEFAULT_LIFE = 3500;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<ToastMessage[]>([]);
  readonly toasts = this._toasts.asReadonly();

  private timers = new Map<string, ReturnType<typeof setTimeout>>();

  success(detail: string, options?: ToastOptions): string {
    return this.show('success', detail, options);
  }

  info(detail: string, options?: ToastOptions): string {
    return this.show('info', detail, options);
  }

  warn(detail: string, options?: ToastOptions): string {
    return this.show('warn', detail, options);
  }

  error(detail: string, options?: ToastOptions): string {
    return this.show('error', detail, options);
  }

  show(severity: ToastSeverity, detail: string, options?: ToastOptions): string {
    const id = crypto.randomUUID();
    const life = options?.life ?? DEFAULT_LIFE;

    const toast: ToastMessage = {
      id,
      severity,
      detail,
      summary: options?.summary,
      life,
      closable: options?.closable ?? true,
    };

    this._toasts.update((toasts) => [...toasts, toast]);

    if (life > 0) {
      this.timers.set(
        id,
        setTimeout(() => this.remove(id), life),
      );
    }

    return id;
  }

  remove(id: string): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
    this._toasts.update((toasts) => toasts.filter((t) => t.id !== id));
  }

  clear(): void {
    this.timers.forEach((timer) => clearTimeout(timer));
    this.timers.clear();
    this._toasts.set([]);
  }
}
