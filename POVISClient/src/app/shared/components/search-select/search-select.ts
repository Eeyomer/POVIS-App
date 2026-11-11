import { ChangeDetectionStrategy, Component, ElementRef, HostListener, computed, forwardRef, inject, input, output, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface SelectOption {
  value: string;
  label: string;
}

/**
 * A select you can type into. Works with formControlName like a normal <select>.
 * The options are whatever the parent gives it; (searched) fires (debounced) with the typed text
 * so the parent can fetch matching rows from the server instead of preloading everything.
 */
@Component({
  selector: 'app-search-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => SearchSelect), multi: true }],
  template: `
    <div class="relative">
      <input type="text" autocomplete="off" role="combobox" [attr.aria-expanded]="open()"
        [placeholder]="placeholder()" [disabled]="disabled()"
        [value]="open() ? term() : selectedLabel()"
        (focus)="openList()" (input)="onType($any($event.target).value)" (keydown)="onKey($event)"
        class="w-full pl-3 pr-14 py-2 rounded-lg border border-[#d3dae5] text-[14px] bg-white disabled:bg-[#f3f5f8]" />
      <div class="absolute inset-y-0 right-2 flex items-center gap-1">
        @if (value() && !disabled()) {
        <button type="button" (mousedown)="$event.preventDefault()" (click)="clear()" title="Clear"
          class="w-6 h-6 rounded-md text-[#7a889c] hover:bg-[#f3f5f8] hover:text-[#152238] flex items-center justify-center">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M18 6 6 18M6 6l12 12" /></svg>
        </button>
        }
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#7a889c" stroke-width="2.5" class="pointer-events-none"><path d="m6 9 6 6 6-6" /></svg>
      </div>

      @if (open()) {
      <ul class="absolute z-30 mt-1 w-full max-h-64 overflow-y-auto rounded-lg border border-[#d3dae5] bg-white shadow-lg py-1 text-[13.5px]"
        role="listbox" (mousedown)="$event.preventDefault()">
        @for (o of options(); track o.value; let i = $index) {
        <li role="option" [attr.aria-selected]="o.value === value()" (click)="pick(o)" (mouseenter)="active.set(i)"
          class="px-3 py-2 cursor-pointer text-[#152238]"
          [class.bg-[#eef4fb]]="i === active()" [class.font-semibold]="o.value === value()">
          {{ o.label }}
        </li>
        } @empty {
        <li class="px-3 py-2 text-[#7a889c]">{{ term() ? 'No matches.' : 'Start typing to search.' }}</li>
        }
      </ul>
      }
    </div>
  `,
})
export class SearchSelect implements ControlValueAccessor {
  options = input<SelectOption[]>([]);
  placeholder = input('Search...');
  /** Typed text (debounced); use it to load matching options. Fires with '' when the list opens. */
  searched = output<string>();

  open = signal(false);
  term = signal('');
  active = signal(0);
  value = signal('');
  disabled = signal(false);
  private pickedLabel = signal('');

  /** Label of the current value: from the options when present, else the one picked last. */
  selectedLabel = computed(() => this.options().find((o) => o.value === this.value())?.label ?? this.pickedLabel());

  private host = inject(ElementRef<HTMLElement>);
  private timer: ReturnType<typeof setTimeout> | null = null;
  private onChange: (v: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
    if (!value) this.pickedLabel.set('');
  }
  registerOnChange(fn: (v: string) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(disabled: boolean): void { this.disabled.set(disabled); }

  openList(): void {
    if (this.open()) return;
    this.term.set('');
    this.active.set(0);
    this.open.set(true);
    this.searched.emit('');
  }

  onType(text: string): void {
    this.term.set(text);
    this.active.set(0);
    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(() => this.searched.emit(text.trim()), 250);
  }

  pick(option: SelectOption): void {
    this.value.set(option.value);
    this.pickedLabel.set(option.label);
    this.onChange(option.value);
    this.close();
  }

  clear(): void {
    this.value.set('');
    this.pickedLabel.set('');
    this.onChange('');
    this.term.set('');
    this.searched.emit('');
  }

  onKey(event: KeyboardEvent): void {
    const count = this.options().length;
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.openList();
      this.active.set(Math.min(this.active() + 1, count - 1));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.active.set(Math.max(this.active() - 1, 0));
    } else if (event.key === 'Enter' && this.open()) {
      event.preventDefault(); // don't submit the form
      const option = this.options()[this.active()];
      if (option) this.pick(option);
    } else if (event.key === 'Escape' && this.open()) {
      event.stopPropagation();
      this.close();
    } else if (event.key === 'Tab') {
      this.close();
    }
  }

  @HostListener('document:mousedown', ['$event'])
  onOutside(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) this.close();
  }

  private close(): void {
    this.open.set(false);
    this.onTouched();
  }
}
