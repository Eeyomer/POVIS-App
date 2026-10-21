import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Prev / numbered / Next page buttons with a "showing x–y of z" summary. */
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (total() > 0) {
    <div class="flex flex-wrap items-center justify-between gap-2 mt-4 text-[12.5px] text-[#7a889c]">
      <span>Showing {{ from() }}–{{ to() }} of {{ total() }}</span>
      @if (totalPages() > 1) {
      <div class="flex flex-wrap gap-1">
        <button type="button" (click)="go(page() - 1)" [disabled]="page() <= 1"
          class="px-3 py-1.5 rounded-lg border border-[#d3dae5] font-semibold text-[#152238] disabled:opacity-40">
          Prev
        </button>
        @for (p of pages(); track $index) {
          @if (p === null) {
          <span class="px-2 py-1.5">…</span>
          } @else {
          <button type="button" (click)="go(p)"
            class="px-3 py-1.5 rounded-lg border font-semibold"
            [class]="p === page() ? 'border-[#1e3a63] bg-[#1e3a63] text-white' : 'border-[#d3dae5] text-[#152238] hover:bg-[#f3f5f8]'">
            {{ p }}
          </button>
          }
        }
        <button type="button" (click)="go(page() + 1)" [disabled]="page() >= totalPages()"
          class="px-3 py-1.5 rounded-lg border border-[#d3dae5] font-semibold text-[#152238] disabled:opacity-40">
          Next
        </button>
      </div>
      }
    </div>
    }
  `,
})
export class Pagination {
  page = input.required<number>();
  pageSize = input(20);
  total = input.required<number>();
  pageChange = output<number>();

  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  to = computed(() => Math.min(this.page() * this.pageSize(), this.total()));

  /** Page numbers to show; null marks a gap. */
  pages = computed<(number | null)[]>(() => {
    const last = this.totalPages();
    const cur = this.page();
    const set = new Set([1, last, cur - 1, cur, cur + 1].filter((p) => p >= 1 && p <= last));
    const sorted = [...set].sort((a, b) => a - b);
    const out: (number | null)[] = [];
    sorted.forEach((p, i) => {
      if (i > 0 && p - sorted[i - 1] > 1) out.push(null);
      out.push(p);
    });
    return out;
  });

  go(p: number): void {
    if (p >= 1 && p <= this.totalPages() && p !== this.page()) this.pageChange.emit(p);
  }
}
