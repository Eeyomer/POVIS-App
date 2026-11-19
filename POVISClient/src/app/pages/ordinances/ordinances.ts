import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { SkeletonRows } from '../../shared/components/skeleton/skeleton';
import { FormPanel } from '../../shared/components/form-panel/form-panel';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrdinancesService } from '../../core/services/ordinances-service';
import { AuthService } from '../../core/services/auth-service';
import { OrdinanceDTO, OrdinanceImportResultDTO, OrdinanceListDTO, OrdinancePenaltyDTO } from '../../core/models/app-dto';
import { ordinal } from '../../core/utils/offense-level';
import { manilaYear } from '../../core/utils/manila-time';
import { ToastService } from '../../shared/toast/toast-service';

/** One ladder of escalating penalty levels: a section, or the ordinance as a whole. */
interface PenaltyScope {
  key: string;
  title: string;
  subtitle: string | null;
  sectionId: string | null;
  penalties: OrdinancePenaltyDTO[];
  nextLevel: number;
}

@Component({
  selector: 'app-ordinances',
  imports: [SkeletonRows, CommonModule, ReactiveFormsModule, FormPanel],
  templateUrl: './ordinances.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Ordinances implements OnInit {
  loading = signal(true);
  ordinances = signal<OrdinanceListDTO[]>([]);
  selected = signal<OrdinanceDTO | null>(null);

  search = signal('');
  filteredOrdinances = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) return this.ordinances();
    return this.ordinances().filter((o) =>
      `${o.ordinanceNumber} ${o.series} ${o.shortName}`.toLowerCase().includes(term),
    );
  });

  showForm = signal(false);
  editing = signal<OrdinanceListDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    ordinanceNumber: ['', Validators.required],
    series: [manilaYear(), Validators.required],
    title: ['', Validators.required],
    shortName: ['', Validators.required],
    isExecutiveOrder: [false],
  });

  sectionForm = this.fb.group({
    sectionCode: ['', Validators.required],
    description: [''],
  });

  levelForm = this.fb.group({
    fineAmount: [null as number | null],
    penaltyText: ['', Validators.required],
  });

  readonly ordinal = ordinal;

  /** Which penalty ladder ("ordinance" or a section id) currently has the add-level form open. */
  addingFor = signal<string | null>(null);

  /** One ladder per section, plus one for the ordinance as a whole. */
  scopes = computed<PenaltyScope[]>(() => {
    const o = this.selected();
    if (!o) return [];
    const build = (key: string, title: string, subtitle: string | null, sectionId: string | null, list: OrdinancePenaltyDTO[]): PenaltyScope => {
      const penalties = [...list].sort((a, b) => a.offenseLevel - b.offenseLevel);
      return { key, title, subtitle, sectionId, penalties, nextLevel: (penalties.at(-1)?.offenseLevel ?? 0) + 1 };
    };
    return [
      build('ordinance', 'Whole ordinance', 'Used when a violation has no section, or its section has no penalties of its own.', null, o.penalties),
      ...o.sections.map((s) =>
        build(s.ordinanceSectionId, s.sectionCode + (s.description ? ' — ' + s.description : ''), null, s.ordinanceSectionId, s.penalties ?? []),
      ),
    ];
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
    this.loading.set(true);
    this.ordinancesService
      .list(true)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((data) => this.ordinances.set(data));
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ series: manilaYear(), isExecutiveOrder: false });
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

  startAdd(key: string): void {
    this.levelForm.reset({ fineAmount: null, penaltyText: '' });
    this.addingFor.set(key);
  }

  cancelAdd(): void {
    this.addingFor.set(null);
  }

  /** Adds the next, higher level to a section's (or the whole ordinance's) penalty ladder. */
  addLevel(scope: PenaltyScope): void {
    const detail = this.selected();
    if (!detail || this.levelForm.invalid) {
      this.levelForm.markAllAsTouched();
      return;
    }
    const raw = this.levelForm.getRawValue();
    const previous = scope.penalties.at(-1);
    if (previous?.fineAmount != null && raw.fineAmount != null && raw.fineAmount < previous.fineAmount) {
      this.toast.error(`The ${ordinal(scope.nextLevel)} offense must be higher than the ${ordinal(previous.offenseLevel)} offense (fine of at least ₱${previous.fineAmount}).`);
      return;
    }
    this.ordinancesService.addPenalty(detail.ordinanceId, {
      ordinanceSectionId: scope.sectionId,
      offenseLevel: scope.nextLevel,
      fineAmount: raw.fineAmount,
      requiresCounseling: false,
      hasImprisonment: false,
      businessPermitAction: 'None',
      isConjunctive: false,
      penaltyText: raw.penaltyText!,
    }).subscribe({
      next: () => {
        this.addingFor.set(null);
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
