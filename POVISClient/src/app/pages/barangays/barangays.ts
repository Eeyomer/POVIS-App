import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BarangaysService } from '../../core/services/barangays-service';
import { AuthService } from '../../core/services/auth-service';
import { BarangayDTO } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-barangays',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './barangays.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Barangays implements OnInit {
  barangays = signal<BarangayDTO[]>([]);
  showForm = signal(false);
  editing = signal<BarangayDTO | null>(null);

  private fb = inject(FormBuilder);

  form = this.fb.group({
    name: ['', Validators.required],
    district: [null as number | null],
  });

  constructor(
    private barangaysService: BarangaysService,
    public authService: AuthService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.barangaysService.list(true).subscribe((data) => this.barangays.set(data));
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ name: '', district: null });
    this.showForm.set(true);
  }

  openEdit(barangay: BarangayDTO): void {
    this.editing.set(barangay);
    this.form.reset({ name: barangay.name, district: barangay.district ?? null });
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

    const { name, district } = this.form.getRawValue();
    const current = this.editing();

    const request = current
      ? this.barangaysService.update(current.barangayId, {
          barangayId: current.barangayId,
          name: name!,
          district,
          isActive: current.isActive,
        })
      : this.barangaysService.create({ name: name!, district });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'Barangay updated.' : 'Barangay added.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  toggleActive(barangay: BarangayDTO): void {
    this.barangaysService
      .update(barangay.barangayId, {
        barangayId: barangay.barangayId,
        name: barangay.name,
        district: barangay.district,
        isActive: !barangay.isActive,
      })
      .subscribe({
        next: () => this.refresh(),
        error: (err) => this.toast.error(err.message),
      });
  }
}
