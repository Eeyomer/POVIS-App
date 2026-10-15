import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ViolatorsService } from '../../core/services/violators-service';
import { BarangaysService } from '../../core/services/barangays-service';
import { AuthService } from '../../core/services/auth-service';
import { BulkUpload } from '../../shared/components/bulk-upload/bulk-upload';
import { BarangayDTO, Gender, ViolatorDTO, ViolatorHistoryDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-violators',
  imports: [CommonModule, ReactiveFormsModule, BulkUpload],
  templateUrl: './violators.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Violators implements OnInit {
  violators = signal<ViolatorDTO[]>([]);
  totalCount = signal(0);
  barangays = signal<BarangayDTO[]>([]);
  search = signal('');
  page = signal(1);
  pageSize = 20;

  showForm = signal(false);
  editing = signal<ViolatorDTO | null>(null);

  historyFor = signal<ViolatorDTO | null>(null);
  history = signal<ViolatorHistoryDTO[]>([]);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    lastName: ['', Validators.required],
    firstName: ['', Validators.required],
    middleName: [''],
    suffix: [''],
    gender: ['Male' as Gender, Validators.required],
    birthday: [''],
    address: [''],
    barangayId: [''],
    city: [''],
    presentedValidId: [''],
    contactNumber: [''],
  });

  constructor(
    private violatorsService: ViolatorsService,
    private barangaysService: BarangaysService,
    public authService: AuthService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.barangaysService.list().subscribe((data) => this.barangays.set(data));
    this.refresh();
  }

  refresh(): void {
    this.violatorsService.search(this.search() || undefined, this.page(), this.pageSize).subscribe((result) => {
      this.violators.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }

  onSearchChange(value: string): void {
    this.search.set(value);
    this.page.set(1);
    this.refresh();
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ gender: 'Male' });
    this.showForm.set(true);
  }

  openEdit(violator: ViolatorDTO): void {
    this.editing.set(violator);
    this.form.reset({
      lastName: violator.lastName,
      firstName: violator.firstName,
      middleName: violator.middleName ?? '',
      suffix: violator.suffix ?? '',
      gender: violator.gender,
      birthday: violator.birthday ?? '',
      address: violator.address ?? '',
      barangayId: violator.barangayId ?? '',
      city: violator.city ?? '',
      presentedValidId: violator.presentedValidId ?? '',
      contactNumber: violator.contactNumber ?? '',
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
    const dto = {
      lastName: raw.lastName!,
      firstName: raw.firstName!,
      middleName: raw.middleName || null,
      suffix: raw.suffix || null,
      gender: raw.gender!,
      birthday: raw.birthday || null,
      address: raw.address || null,
      barangayId: raw.barangayId || null,
      city: raw.city || null,
      presentedValidId: raw.presentedValidId || null,
      contactNumber: raw.contactNumber || null,
    };

    const current = this.editing();
    const request = current
      ? this.violatorsService.update(current.violatorId, { violatorId: current.violatorId, ...dto })
      : this.violatorsService.create(dto);

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Violator updated.' : 'Violator added.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  viewHistory(violator: ViolatorDTO): void {
    this.historyFor.set(violator);
    this.violatorsService.getHistory(violator.violatorId).subscribe((data) => this.history.set(data));
  }

  closeHistory(): void {
    this.historyFor.set(null);
    this.history.set([]);
  }
}
