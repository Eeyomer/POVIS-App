import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ViolationsService } from '../../core/services/violations-service';
import { ViolatorsService } from '../../core/services/violators-service';
import { OrdinancesService } from '../../core/services/ordinances-service';
import { BarangaysService } from '../../core/services/barangays-service';
import {
  BarangayDTO,
  Division,
  OrdinanceDTO,
  OrdinanceListDTO,
  ViolationDTO,
  ViolationStatus,
  ViolatorDTO,
} from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-violations',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './violations.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Violations implements OnInit {
  violations = signal<ViolationDTO[]>([]);
  totalCount = signal(0);
  violators = signal<ViolatorDTO[]>([]);
  ordinances = signal<OrdinanceListDTO[]>([]);
  barangays = signal<BarangayDTO[]>([]);
  selectedOrdinanceDetail = signal<OrdinanceDTO | null>(null);

  showForm = signal(false);
  editing = signal<ViolationDTO | null>(null);

  statusFilter = signal<ViolationStatus | ''>('');

  private fb = inject(FormBuilder);

  form = this.fb.group({
    violatorId: ['', Validators.required],
    ovrNumber: ['', Validators.required],
    dateOfSubmission: ['', Validators.required],
    division: ['Bpd' as Division, Validators.required],
    ageAtApprehension: [null as number | null],
    dateOfOccurrence: ['', Validators.required],
    timeOfOccurrence: [''],
    placeOfOccurrence: [''],
    barangayOfOccurrenceId: [''],
    witness: [''],
    apprehendingOfficer: [''],
    ordinanceId: ['', Validators.required],
    ordinanceSectionId: [''],
    isWarningTicket: [false],
    remarks: [''],
    remarks2: [''],
  });

  constructor(
    private violationsService: ViolationsService,
    private violatorsService: ViolatorsService,
    private ordinancesService: OrdinancesService,
    private barangaysService: BarangaysService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.violatorsService.search(undefined, 1, 200).subscribe((res) => this.violators.set(res.items));
    this.ordinancesService.list().subscribe((data) => this.ordinances.set(data));
    this.barangaysService.list().subscribe((data) => this.barangays.set(data));
    this.refresh();
  }

  refresh(): void {
    this.violationsService.list({ status: this.statusFilter() || undefined }).subscribe((result) => {
      this.violations.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter.set(value as ViolationStatus | '');
    this.refresh();
  }

  onOrdinanceChange(ordinanceId: string): void {
    if (!ordinanceId) {
      this.selectedOrdinanceDetail.set(null);
      return;
    }
    this.ordinancesService.getById(ordinanceId).subscribe((full) => this.selectedOrdinanceDetail.set(full));
  }

  openAdd(): void {
    this.editing.set(null);
    this.selectedOrdinanceDetail.set(null);
    this.form.reset({ division: 'Bpd', isWarningTicket: false });
    this.showForm.set(true);
  }

  openEdit(violation: ViolationDTO): void {
    this.editing.set(violation);
    this.onOrdinanceChange(violation.ordinanceId);
    this.form.reset({
      violatorId: violation.violatorId,
      ovrNumber: violation.ovrNumber,
      dateOfSubmission: violation.dateOfSubmission,
      division: violation.division,
      ageAtApprehension: violation.ageAtApprehension ?? null,
      dateOfOccurrence: violation.dateOfOccurrence,
      timeOfOccurrence: violation.timeOfOccurrence ?? '',
      placeOfOccurrence: violation.placeOfOccurrence ?? '',
      barangayOfOccurrenceId: violation.barangayOfOccurrenceId ?? '',
      witness: violation.witness ?? '',
      apprehendingOfficer: violation.apprehendingOfficer ?? '',
      ordinanceId: violation.ordinanceId,
      ordinanceSectionId: violation.ordinanceSectionId ?? '',
      isWarningTicket: violation.isWarningTicket,
      remarks: violation.remarks ?? '',
      remarks2: violation.remarks2 ?? '',
    });
    this.showForm.set(true);
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
      ? this.violationsService.update(current.violationId, {
          violationId: current.violationId,
          dateOfSubmission: raw.dateOfSubmission!,
          division: raw.division!,
          ageAtApprehension: raw.ageAtApprehension,
          dateOfOccurrence: raw.dateOfOccurrence!,
          timeOfOccurrence: raw.timeOfOccurrence || null,
          placeOfOccurrence: raw.placeOfOccurrence || null,
          barangayOfOccurrenceId: raw.barangayOfOccurrenceId || null,
          witness: raw.witness || null,
          apprehendingOfficer: raw.apprehendingOfficer || null,
          ordinanceSectionId: raw.ordinanceSectionId || null,
          status: current.status,
          remarks: raw.remarks || null,
          remarks2: raw.remarks2 || null,
        })
      : this.violationsService.create({
          violatorId: raw.violatorId!,
          ovrNumber: raw.ovrNumber!,
          dateOfSubmission: raw.dateOfSubmission!,
          division: raw.division!,
          ageAtApprehension: raw.ageAtApprehension,
          dateOfOccurrence: raw.dateOfOccurrence!,
          timeOfOccurrence: raw.timeOfOccurrence || null,
          placeOfOccurrence: raw.placeOfOccurrence || null,
          barangayOfOccurrenceId: raw.barangayOfOccurrenceId || null,
          witness: raw.witness || null,
          apprehendingOfficer: raw.apprehendingOfficer || null,
          ordinanceId: raw.ordinanceId!,
          ordinanceSectionId: raw.ordinanceSectionId || null,
          isWarningTicket: !!raw.isWarningTicket,
          remarks: raw.remarks || null,
          remarks2: raw.remarks2 || null,
        });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Violation updated.' : 'Violation recorded.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  remove(violation: ViolationDTO): void {
    if (!confirm(`Delete violation ${violation.ovrNumber}?`)) return;
    this.violationsService.remove(violation.violationId).subscribe({
      next: () => {
        this.toast.success('Violation deleted.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }
}
