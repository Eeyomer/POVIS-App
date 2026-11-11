import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, HostListener, input, output, signal, viewChild } from '@angular/core';
import { renderAsync } from 'docx-preview';
import { GeneratedDocument } from '../../../core/services/document-service';

/** Modal that renders a generated Word document so it can be checked before downloading. */
@Component({
  selector: 'app-doc-preview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4" (click)="closed.emit()">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-5xl max-h-[92vh] flex flex-col"
        role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
        <div class="flex flex-wrap items-center justify-between gap-3 px-6 py-4 border-b border-[#eef2f8]">
          <h2 class="text-[15px] font-semibold text-[#152238]">{{ document().title }} — Preview</h2>
          <div class="flex gap-3">
            <button type="button" (click)="downloaded.emit()"
              class="px-4 py-2 rounded-lg bg-[#1e3a63] text-white text-[13px] font-semibold hover:bg-[#284d82]">Download</button>
            <button type="button" (click)="closed.emit()"
              class="px-4 py-2 rounded-lg border border-[#d3dae5] text-[13px] font-semibold text-[#152238]">Close</button>
          </div>
        </div>
        <div class="overflow-auto bg-[#e8ecf2] p-4 flex-1">
          @if (failed()) {
          <p class="text-[13px] text-red-500 p-4">The preview could not be displayed. You can still download the document.</p>
          }
          <div #host></div>
        </div>
      </div>
    </div>
  `,
})
export class DocPreview implements AfterViewInit {
  document = input.required<GeneratedDocument>();
  closed = output<void>();
  downloaded = output<void>();

  failed = signal(false);
  private host = viewChild.required<ElementRef<HTMLElement>>('host');

  async ngAfterViewInit(): Promise<void> {
    try {
      await renderAsync(this.document().blob, this.host().nativeElement, undefined, { inWrapper: true, breakPages: true });
    } catch {
      this.failed.set(true);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closed.emit();
  }
}
