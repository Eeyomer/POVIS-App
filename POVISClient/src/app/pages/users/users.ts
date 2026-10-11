import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsersService } from '../../core/services/users-service';
import { AuthService } from '../../core/services/auth-service';
import { ROLE_LABELS, Role, User } from '../../core/models/app-dto';
import { ToastService } from '../../shared/toast/toast-service';

@Component({
  selector: 'app-users',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './users.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Users implements OnInit {
  users = signal<User[]>([]);
  search = signal('');
  filteredUsers = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) return this.users();
    return this.users().filter((u) =>
      `${u.username} ${ROLE_LABELS[u.role]} ${u.deputationNumber ?? ''}`.toLowerCase().includes(term),
    );
  });
  showForm = signal(false);
  editing = signal<User | null>(null);
  resettingId = signal<string | null>(null);

  roles: Role[] = ['MISPersonnel', 'OVRSupervisor', 'ApprehendingOfficer', 'PNPBarangayStaff'];
  roleLabels = ROLE_LABELS;

  private fb = inject(FormBuilder);

  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
    role: ['ApprehendingOfficer' as Role, Validators.required],
    deputationNumber: ['', [Validators.maxLength(50)]],
  });

  resetForm = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor(
    private usersService: UsersService,
    public authService: AuthService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.form.controls.role.valueChanges.subscribe(() => this.syncDeputationValidator());
    this.refresh();
  }

  refresh(): void {
    this.usersService.list().subscribe((data) => this.users.set(data));
  }

  openAdd(): void {
    this.editing.set(null);
    this.form.reset({ username: '', password: '', role: 'ApprehendingOfficer', deputationNumber: '' });
    this.form.get('password')?.setValidators([Validators.required, Validators.minLength(8)]);
    this.form.get('password')?.updateValueAndValidity();
    this.syncDeputationValidator();
    this.showForm.set(true);
  }

  openEdit(user: User): void {
    this.editing.set(user);
    this.form.reset({ username: user.username, password: '', role: user.role, deputationNumber: user.deputationNumber ?? '' });
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
    this.syncDeputationValidator();
    this.showForm.set(true);
  }

  isOfficer(): boolean {
    return this.form.controls.role.value === 'ApprehendingOfficer';
  }

  /** Deputation number is mandatory for Apprehending Officers only. */
  private syncDeputationValidator(): void {
    const control = this.form.controls.deputationNumber;
    control.setValidators(this.isOfficer() ? [Validators.required, Validators.maxLength(50)] : [Validators.maxLength(50)]);
    control.updateValueAndValidity();
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

    const { username, password, role, deputationNumber } = this.form.getRawValue();
    const deputation = role === 'ApprehendingOfficer' ? deputationNumber?.trim() || null : null;
    const current = this.editing();

    const request = current
      ? this.usersService.update(current.id, { id: current.id, role: role!, isActive: current.isActive, deputationNumber: deputation })
      : this.usersService.create({ username: username!, password: password!, role: role!, deputationNumber: deputation });

    request.subscribe({
      next: () => {
        this.toast.success(current ? 'User updated.' : 'User created.');
        this.cancel();
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  toggleActive(user: User): void {
    this.usersService.update(user.id, { id: user.id, role: user.role, isActive: !user.isActive, deputationNumber: user.deputationNumber }).subscribe({
      next: () => this.refresh(),
      error: (err) => this.toast.error(err.message),
    });
  }

  openResetPassword(user: User): void {
    this.resettingId.set(user.id);
    this.resetForm.reset({ newPassword: '' });
  }

  cancelResetPassword(): void {
    this.resettingId.set(null);
  }

  submitResetPassword(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    const id = this.resettingId();
    if (!id) return;

    this.usersService.resetPassword(id, { newPassword: this.resetForm.getRawValue().newPassword! }).subscribe({
      next: () => {
        this.toast.success('Password reset.');
        this.cancelResetPassword();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  remove(user: User): void {
    this.usersService.remove(user.id).subscribe({
      next: () => {
        this.toast.success('User removed.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }
}
