import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';

/** Shows the first validation message for a control once it is touched (or the form was submitted). */
@Component({
  selector: 'app-field-error',
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    @if (control().touched && control().invalid) {
      <p class="mt-1 text-[12px] text-[#c81e5c]" role="alert">{{ message() }}</p>
    }
  `,
})
export class FieldError {
  control = input.required<AbstractControl>();
  label = input<string>('This field');

  message(): string {
    const e = this.control().errors;
    if (!e) return '';
    const label = this.label();
    if (e['required']) return `${label} is required.`;
    if (e['email']) return 'Enter a valid email address.';
    if (e['minlength']) return `${label} must be at least ${e['minlength'].requiredLength} characters.`;
    if (e['maxlength']) return `${label} must be at most ${e['maxlength'].requiredLength} characters.`;
    if (e['min']) return `${label} must be at least ${e['min'].min}.`;
    if (e['max']) return `${label} must be at most ${e['max'].max}.`;
    if (e['pattern']) return `${label} is not in a valid format.`;
    return `${label} is invalid.`;
  }
}
