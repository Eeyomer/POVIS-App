import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrdinancesService } from '../../core/services/ordinances-service';
import { AuthService } from '../../core/services/auth-service';
import { OffenseLevel, OrdinanceDTO, OrdinanceImportResultDTO, OrdinanceListDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-ordinances',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './ordinances.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Ordinances implements OnInit {
  ordinances = signal<OrdinanceListDTO[]>([]);
  selected = signal<OrdinanceDTO | null>(null);

  showForm = signal(false);
  editing = signal<OrdinanceListDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    ordinanceNumber: ['', Validators.required],
    series: [new Date().getFullYear(), Validators.required],
    title: ['', Validators.required],
    shortName: ['', Validators.required],
    isExecutiveOrder: [false],
  });

  sectionForm = this.fb.group({
    sectionCode: ['', Validators.required],
    description: [''],
  });

  penaltyForm = this.fb.group({
    ordinanceSectionId: [''],
    offenseLevel: ['First' as OffenseLevel, Validators.required],
    fineAmount: [null as number | null],
    penaltyText: ['', Validators.required],
  });

  constructor(
    private ordinancesService: OrdinancesService,
    public authService: AuthService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.refresh();
  }

  uploading = signal(false);
  importResult = signal<OrdinanceImportResultDTO | null>(null);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.uploading.set(true);
    this.importResult.set(null);
    this.ordinancesService.bulkUpload(file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.importResult.set(result);
        this.toast.success('Ordinances imported.');
        this.refresh();
      },
      error: (err: Error & { result?: OrdinanceImportResultDTO }) => {
        this.uploading.set(false);
        this.importResult.set(err.result ?? null);
        this.toast.error(err.message);
      },
    });
  }

  dismissImportResult(): void {
    this.importResult.set(null);
  }

  refresh(): void {
    this.ordinancesService.list(true).subscribe((data) => this.ordinances.set(data));
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ series: new Date().getFullYear(), isExecutiveOrder: false });
    this.showForm.set(true);
  }

  openEdit(ordinance: OrdinanceListDTO): void {
    this.editing.set(ordinance);
    this.ordinancesService.getById(ordinance.ordinanceId).subscribe((full) => {
      if (!full) return;
      this.form.reset({
        ordinanceNumber: full.ordinanceNumber,
        series: full.series,
        title: full.title,
        shortName: full.shortName,
        isExecutiveOrder: full.isExecutiveOrder,
      });
      this.showForm.set(true);
    });
  }

  cancel(): void {
    this.showForm.set(false);
    this.editing.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const current = this.editing();
    const request = current
      ? this.ordinancesService.update(current.ordinanceId, {
          ordinanceId: current.ordinanceId,
          ordinanceNumber: raw.ordinanceNumber!,
          series: raw.series!,
          title: raw.title!,
          shortName: raw.shortName!,
          isExecutiveOrder: !!raw.isExecutiveOrder,
          isActive: current.isActive,
        })
      : this.ordinancesService.create({
          ordinanceNumber: raw.ordinanceNumber!,
          series: raw.series!,
          title: raw.title!,
          shortName: raw.shortName!,
          isExecutiveOrder: !!raw.isExecutiveOrder,
        });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Ordinance updated.' : 'Ordinance added.');
        this.cancel();
        this.refresh();
        if (this.selected()?.ordinanceId === current?.ordinanceId) this.viewDetail(current!);
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  viewDetail(ordinance: OrdinanceListDTO): void {
    this.ordinancesService.getById(ordinance.ordinanceId).subscribe((full) => this.selected.set(full));
  }

  closeDetail(): void {
    this.selected.set(null);
  }

  addSection(): void {
    const detail = this.selected();
    if (!detail || this.sectionForm.invalid) {
      this.sectionForm.markAllAsTouched();
      return;
    }
    const raw = this.sectionForm.getRawValue();
    this.ordinancesService.addSection(detail.ordinanceId, { sectionCode: raw.sectionCode!, description: raw.description || null })
      .subscribe({
        next: () => {
          this.sectionForm.reset();
          this.viewDetail({ ordinanceId: detail.ordinanceId } as OrdinanceListDTO);
        },
        error: (err) => this.toast.error(err.message),
      });
  }

  removeSection(sectionId: string): void {
    const detail = this.selected();
    if (!detail) return;
    this.ordinancesService.removeSection(detail.ordinanceId, sectionId).subscribe({
      next: () => this.viewDetail({ ordinanceId: detail.ordinanceId } as OrdinanceListDTO),
      error: (err) => this.toast.error(err.message),
    });
  }

  addPenalty(): void {
    const detail = this.selected();
    if (!detail || this.penaltyForm.invalid) {
      this.penaltyForm.markAllAsTouched();
      return;
    }
    const raw = this.penaltyForm.getRawValue();
    this.ordinancesService.addPenalty(detail.ordinanceId, {
      ordinanceSectionId: raw.ordinanceSectionId || null,
      offenseLevel: raw.offenseLevel!,
      fineAmount: raw.fineAmount,
      requiresCounseling: false,
      hasImprisonment: false,
      businessPermitAction: 'None',
      isConjunctive: false,
      penaltyText: raw.penaltyText!,
    }).subscribe({
      next: () => {
        this.penaltyForm.reset({ offenseLevel: 'First' });
        this.viewDetail({ ordinanceId: detail.ordinanceId } as OrdinanceListDTO);
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  removePenalty(penaltyId: string): void {
    const detail = this.selected();
    if (!detail) return;
    this.ordinancesService.removePenalty(detail.ordinanceId, penaltyId).subscribe({
      next: () => this.viewDetail({ ordinanceId: detail.ordinanceId } as OrdinanceListDTO),
      error: (err) => this.toast.error(err.message),
    });
  }
}
