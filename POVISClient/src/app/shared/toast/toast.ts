import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from './toast-service';
import { ToastMessage, ToastSeverity } from './toast.model';

interface ToastStyle {
  bg: string;
  border: string;
  iconColor: string;
}

const TOAST_STYLES: Record<ToastSeverity, ToastStyle> = {
  success: { bg: 'bg-[#f0fbf4]', border: 'border-[#34c77b]', iconColor: 'text-[#1f9d5c]' },
  info: { bg: 'bg-[#f4eefa]', border: 'border-[#5b1e8c]', iconColor: 'text-[#5b1e8c]' },
  warn: { bg: 'bg-[#fff8ec]', border: 'border-[#f5a524]', iconColor: 'text-[#b45309]' },
  error: { bg: 'bg-[#fdf0f3]', border: 'border-[#e0417a]', iconColor: 'text-[#c81e5c]' },
};

@Component({
  selector: 'app-toast',
  imports: [CommonModule],
  templateUrl: './toast.html',
  styleUrl: './toast.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Toast {
  private readonly toastService = inject(ToastService);

  readonly toasts = this.toastService.toasts;

  styleFor(severity: ToastSeverity): ToastStyle {
    return TOAST_STYLES[severity];
  }

  close(toast: ToastMessage): void {
    this.toastService.remove(toast.id);
  }
}
