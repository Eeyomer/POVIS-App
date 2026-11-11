import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** A single pulsing placeholder block. Size it from the outside, e.g. `<app-skeleton class="h-4 w-24" />`. */
@Component({
  selector: 'app-skeleton',
  template: '',
  host: {
    class: 'block animate-pulse rounded-md bg-[#e3e9f2]',
    'aria-hidden': 'true',
  },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Skeleton {}

/**
 * Placeholder table rows shown while a list loads.
 * Usage: `<tr app-skeleton-rows [columns]="4" [rows]="6"></tr>` inside a `<tbody>`.
 */
@Component({
  selector: 'tbody[app-skeleton-rows]',
  template: `
    @for (r of rowList(); track r) {
    <tr class="border-t border-[#eef2f8]">
      @for (c of colList(); track c) {
      <td class="px-4 py-3"><app-skeleton class="h-4" [style.width.%]="widthFor(r, c)" /></td>
      }
    </tr>
    }
  `,
  imports: [Skeleton],
  host: { role: 'status', 'aria-label': 'Loading' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonRows {
  columns = input(4);
  rows = input(6);

  rowList(): number[] {
    return Array.from({ length: this.rows() }, (_, i) => i);
  }

  colList(): number[] {
    return Array.from({ length: this.columns() }, (_, i) => i);
  }

  /** Varies widths a little so the rows don't look like a uniform grid. */
  widthFor(row: number, col: number): number {
    return 45 + ((row * 3 + col * 5) % 5) * 11;
  }
}
