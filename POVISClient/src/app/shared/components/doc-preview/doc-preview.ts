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
            <button type="button" (click)="savePdf()"
              class="px-4 py-2 rounded-lg bg-[#1e3a63] text-white text-[13px] font-semibold hover:bg-[#284d82]">Save as PDF</button>
            <button type="button" (click)="downloaded.emit()"
              class="px-4 py-2 rounded-lg border border-[#d3dae5] text-[13px] font-semibold text-[#152238]">Download Word</button>
            <button type="button" (click)="closed.emit()"
              class="px-4 py-2 rounded-lg border border-[#d3dae5] text-[13px] font-semibold text-[#152238]">Close</button>
          </div>
        </div>
        <div class="overflow-auto bg-[#e8ecf2] p-4 flex-1">
          @if (failed()) {
          <p class="text-[13px] text-red-500 p-4">The preview could not be displayed. You can still download the document.</p>
          }
          <div #host class="select-none pointer-events-none" contenteditable="false"></div>
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

  /** Opens the browser's print dialog for the rendered pages only; choose "Save as PDF". */
  savePdf(): void {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;width:0;height:0;border:0;visibility:hidden';
    document.body.appendChild(frame);
    const doc = frame.contentDocument!;
    const styles = Array.from(this.host().nativeElement.querySelectorAll('style')).map((s) => s.outerHTML).join('');
    const pages = this.host().nativeElement.innerHTML.replace(/<style[\s\S]*?<\/style>/g, '');
    doc.open();
    doc.write(`<!doctype html><title>${this.document().title}</title>${styles}
      <style>@page{margin:0}body{margin:0}.docx-wrapper{background:none!important;padding:0!important}
      .docx-wrapper>section.docx{box-shadow:none!important;margin:0!important}</style>${pages}`);
    doc.close();
    frame.contentWindow!.onafterprint = () => frame.remove();
    setTimeout(() => frame.contentWindow!.print(), 300);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closed.emit();
  }
}
