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
  /** Temporary password issued by the system; shown once so MIS Personnel can hand it over. */
  issuedPassword = signal<{ username: string; password: string } | null>(null);

  roles: Role[] = ['MISPersonnel', 'OVRSupervisor', 'ApprehendingOfficer', 'PNPBarangayStaff'];
  roleLabels = ROLE_LABELS;

  private fb = inject(FormBuilder);

  form = this.fb.group({
    username: ['', Validators.required],
    role: ['ApprehendingOfficer' as Role, Validators.required],
    deputationNumber: ['', [Validators.maxLength(50)]],
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
    this.form.reset({ username: '', role: 'ApprehendingOfficer', deputationNumber: '' });
    this.syncDeputationValidator();
    this.showForm.set(true);
  }

  openEdit(user: User): void {
    this.editing.set(user);
    this.form.reset({ username: user.username, role: user.role, deputationNumber: user.deputationNumber ?? '' });
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

    const { username, role, deputationNumber } = this.form.getRawValue();
    const deputation = role === 'ApprehendingOfficer' ? deputationNumber?.trim() || null : null;
    const current = this.editing();

    const request = current
      ? this.usersService.update(current.id, { id: current.id, role: role!, isActive: current.isActive, deputationNumber: deputation })
      : this.usersService.create({ username: username!, role: role!, deputationNumber: deputation });

    request.subscribe({
      next: (saved) => {
        this.toast.success(current ? 'User updated.' : 'User created.');
        if (!current && saved.temporaryPassword) {
          this.issuedPassword.set({ username: saved.username, password: saved.temporaryPassword });
        }
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

  resetPassword(user: User): void {
    if (!confirm(`Reset the password of ${user.username}? They will have to change it on their next login.`)) return;
    this.usersService.resetPassword(user.id).subscribe({
      next: (temporaryPassword) => {
        this.issuedPassword.set({ username: user.username, password: temporaryPassword });
        this.toast.success('Temporary password generated.');
        this.refresh();
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  copyIssuedPassword(): void {
    const issued = this.issuedPassword();
    if (!issued) return;
    navigator.clipboard?.writeText(issued.password).then(
      () => this.toast.success('Password copied.'),
      () => this.toast.error('Could not copy. Please copy it manually.'),
    );
  }

  dismissIssuedPassword(): void {
    this.issuedPassword.set(null);
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
