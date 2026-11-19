import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { SkeletonRows } from '../../shared/components/skeleton/skeleton';
import { FormPanel } from '../../shared/components/form-panel/form-panel';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrdinancesService } from '../../core/services/ordinances-service';
import { AuthService } from '../../core/services/auth-service';
import { OrdinanceDTO, OrdinanceImportResultDTO, OrdinanceListDTO, OrdinancePenaltyDTO, OrdinanceSectionDTO } from '../../core/models/app-dto';
import { ordinal } from '../../core/utils/offense-level';
import { manilaYear } from '../../core/utils/manila-time';
import { ToastService } from '../../shared/toast/toast-service';

/** One ladder of escalating penalty levels: a section, or the ordinance as a whole. */
interface PenaltyScope {
  key: string;
  title: string;
  subtitle: string | null;
  sectionId: string | null;
  section: OrdinanceSectionDTO | null;
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
    description: ['', Validators.maxLength(5000)],
  });

  levelForm = this.fb.group({
    fineAmount: [null as number | null],
    penaltyText: [''],
  });

  readonly ordinal = ordinal;

  /** Which accordion item ("ordinance" or a section id) is expanded. */
  openScope = signal<string | null>(null);

  toggleScope(key: string): void {
    this.openScope.update((cur) => (cur === key ? null : key));
  }

  /** Which penalty ladder ("ordinance" or a section id) currently has the add-level form open. */
  addingFor = signal<string | null>(null);

  /** One ladder per section, plus one for the ordinance as a whole. */
  scopes = computed<PenaltyScope[]>(() => {
    const o = this.selected();
    if (!o) return [];
    const build = (key: string, title: string, subtitle: string | null, section: OrdinanceSectionDTO | null, list: OrdinancePenaltyDTO[]): PenaltyScope => {
      const penalties = [...list].sort((a, b) => a.offenseLevel - b.offenseLevel);
      return { key, title, subtitle, sectionId: section?.ordinanceSectionId ?? null, section, penalties, nextLevel: (penalties.at(-1)?.offenseLevel ?? 0) + 1 };
    };
    return [
      build('ordinance', 'Whole ordinance', 'Used when a violation has no section, or its section has no penalties of its own.', null, o.penalties),
      ...o.sections.map((s) => build(s.ordinanceSectionId, s.sectionCode, null, s, s.penalties ?? [])),
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
    this.openScope.set(null);
    this.showSectionForm.set(false);
  }

  /** The section being edited, or null while adding a new one. */
  editingSectionId = signal<string | null>(null);

  /** The section form stays hidden until the user clicks "Add section" or edits one. */
  showSectionForm = signal(false);

  editSection(section: OrdinanceSectionDTO): void {
    this.editingSectionId.set(section.ordinanceSectionId);
    this.showSectionForm.set(true);
    this.sectionForm.reset({ sectionCode: section.sectionCode, description: section.description ?? '' });
  }

  cancelSectionEdit(): void {
    this.editingSectionId.set(null);
    this.showSectionForm.set(false);
    this.sectionForm.reset();
  }

  addSection(): void {
    const detail = this.selected();
    if (!detail || this.sectionForm.invalid) {
      this.sectionForm.markAllAsTouched();
      return;
    }
    const raw = this.sectionForm.getRawValue();
    const dto = { sectionCode: raw.sectionCode!.trim(), description: raw.description?.trim() || null };
    const editingId = this.editingSectionId();
    const request = editingId
      ? this.ordinancesService.updateSection(detail.ordinanceId, editingId, dto)
      : this.ordinancesService.addSection(detail.ordinanceId, dto);
    request.subscribe({
      next: () => {
        this.toast.success(editingId ? 'Section updated.' : 'Section added.');
        this.cancelSectionEdit();
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

  levelError = signal<string | null>(null);

  /** The penalty being edited, or null while adding the next level. */
  editingPenaltyId = signal<string | null>(null);

  editPenalty(scope: PenaltyScope, penalty: OrdinancePenaltyDTO): void {
    this.levelError.set(null);
    this.editingPenaltyId.set(penalty.ordinancePenaltyId);
    this.levelForm.reset({ fineAmount: penalty.fineAmount ?? null, penaltyText: penalty.penaltyText });
    this.addingFor.set(scope.key);
  }

  startAdd(key: string): void {
    this.editingPenaltyId.set(null);
    this.levelError.set(null);
    this.levelForm.reset({ fineAmount: null, penaltyText: '' });
    this.addingFor.set(key);
  }

  cancelAdd(): void {
    this.editingPenaltyId.set(null);
    this.addingFor.set(null);
  }

  /** Adds the next, higher level to a section's (or the whole ordinance's) penalty ladder. */
  addLevel(scope: PenaltyScope): void {
    const detail = this.selected();
    if (!detail) return;
    const raw = this.levelForm.getRawValue();
    const text = raw.penaltyText?.trim() ?? '';
    if (raw.fineAmount == null && !text) {
      this.levelError.set('Enter at least one penalty: a fine amount or a penalty description.');
      return;
    }
    this.levelError.set(null);

    const editingId = this.editingPenaltyId();
    if (editingId) {
      this.ordinancesService.updatePenalty(detail.ordinanceId, editingId, {
        fineAmount: raw.fineAmount,
        penaltyText: text || `Fine of ₱${raw.fineAmount}`,
      }).subscribe({
        next: () => {
          this.toast.success('Penalty updated.');
          this.cancelAdd();
          this.viewDetail({ ordinanceId: detail.ordinanceId } as OrdinanceListDTO);
        },
        error: (err) => this.levelError.set(err.message),
      });
      return;
    }

    const previous = scope.penalties.at(-1);
    if (previous?.fineAmount != null && raw.fineAmount != null && raw.fineAmount < previous.fineAmount) {
      this.levelError.set(`The ${ordinal(scope.nextLevel)} offense must be higher than the ${ordinal(previous.offenseLevel)} offense (fine of at least ₱${previous.fineAmount}).`);
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
      penaltyText: text || `Fine of ₱${raw.fineAmount}`,
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
