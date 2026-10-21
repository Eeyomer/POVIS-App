import { ChangeDetectionStrategy, Component, HostListener, inject, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { ViewModeService } from '../../../core/services/view-mode-service';

/**
 * Wraps a create/edit/view panel. Renders inline by default, or as a modal dialog when
 * MIS Personnel has switched on "Modal forms". Backdrop click and Escape emit `dismissed`.
 */
@Component({
  selector: 'app-form-panel',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template #body><ng-content /></ng-template>
    @if (viewMode.modalForms()) {
    <div class="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4" (click)="dismissed.emit()">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-4xl max-h-[90vh] overflow-y-auto p-6"
        role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
        <ng-container [ngTemplateOutlet]="body" />
      </div>
    </div>
    } @else {
    <div class="bg-white rounded-2xl shadow-sm p-6 mb-6">
      <ng-container [ngTemplateOutlet]="body" />
    </div>
    }
  `,
})
export class FormPanel {
  viewMode = inject(ViewModeService);
  dismissed = output<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.viewMode.modalForms()) this.dismissed.emit();
  }
}
