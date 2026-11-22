import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MaritalStatus } from '../../core/models/app-dto';
import { IMAGE_ACCEPT, readImageAsDataUrl } from '../../core/utils/image-upload';
import { ToastService } from '../../shared/toast/toast-service';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/services/auth-service';
import { SidebarService } from '../../core/services/sidebar-service';

type NavGroup = 'records' | 'enforcement' | 'compliance' | 'reports' | 'administration';

/** The group each page belongs to; the dashboard is a standalone link. */
const GROUP_BY_PATH: Record<string, NavGroup> = {
  barangays: 'records',
  violators: 'records',
  ordinances: 'records',
  verify: 'records',
  violations: 'enforcement',
  'filed-cases': 'enforcement',
  payments: 'enforcement',
  incentives: 'enforcement',
  'seminar-records': 'compliance',
  'counseling-records': 'compliance',
  'community-service-records': 'compliance',
  reports: 'reports',
  users: 'administration',
  system: 'administration',
};

@Component({
  selector: 'app-admin-sidebar',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sidebar implements OnInit {
  username: string | null = null;
  roleLabel = '';

  /** Only one group is expanded at a time so the sidebar never needs to scroll. */
  openGroup = signal<NavGroup | null>(null);

  private destroyRef = inject(DestroyRef);

  constructor(
    public authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    public sidebarService: SidebarService,
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername();
    this.authService.loadProfile();
    this.roleLabel = this.authService.getRoleLabel();
    this.cdr.markForCheck();

    this.openGroupForUrl(this.router.url);
    // Follow the route (e.g. a link inside a page) so the active item is always visible.
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd), takeUntilDestroyed(this.destroyRef))
      .subscribe((e) => this.openGroupForUrl(e.urlAfterRedirects));
  }

  toggleGroup(group: NavGroup): void {
    this.openGroup.update((current) => (current === group ? null : group));
  }

  private openGroupForUrl(url: string): void {
    const group = GROUP_BY_PATH[url.split(/[?#]/)[0].split('/').filter(Boolean)[0] ?? ''];
    if (group) this.openGroup.set(group);
  }

  /** Name shown in the profile card: the full name when set, otherwise the username. */
  displayName(): string {
    return this.authService.profile()?.fullName?.trim() || this.username || 'User';
  }

  /** Shown in the circle when no photo was uploaded: first letters of the first two words of the name. */
  initials(): string {
    const name = this.authService.profile()?.fullName?.trim();
    if (name) {
      return name.split(/s+/).slice(0, 2).map((p) => p[0]?.toUpperCase() ?? '').join('');
    }
    return (this.username ?? 'A').trim().slice(0, 2).toUpperCase();
  }

  onNavigate(): void {
    this.sidebarService.close();
  }

  // ----- Profile modal -----
  showProfile = signal(false);
  savingProfile = signal(false);
  profilePhoto = signal<string | null>(null);
  maritalStatuses: MaritalStatus[] = ['Single', 'Married', 'Widowed', 'Separated', 'Divorced'];
  imageAccept = IMAGE_ACCEPT;
  private photoChanged = false;

  profileForm = inject(FormBuilder).group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    contactNumber: ['', [Validators.maxLength(30)]],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    address: ['', [Validators.maxLength(300)]],
    maritalStatus: [''],
  });

  private toast = inject(ToastService);

  openProfile(): void {
    this.photoChanged = false;
    this.authService.getProfile().subscribe({
      next: (u) => {
        this.profileForm.reset({
          fullName: u.fullName ?? '',
          contactNumber: u.contactNumber ?? '',
          email: u.email ?? '',
          address: u.address ?? '',
          maritalStatus: u.maritalStatus ?? '',
        });
        this.profilePhoto.set(u.photo ?? null);
        this.showProfile.set(true);
      },
      error: (err) => this.toast.error(err.message),
    });
  }

  closeProfile(): void {
    this.showProfile.set(false);
  }

  async onProfilePhotoSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    try {
      this.profilePhoto.set(await readImageAsDataUrl(file, { maxDimension: 400, maxBytes: 900_000 }));
      this.photoChanged = true;
    } catch (e) {
      this.toast.error((e as Error).message);
    }
  }

  removeProfilePhoto(): void {
    this.profilePhoto.set(null);
    this.photoChanged = true;
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    const v = this.profileForm.getRawValue();
    this.savingProfile.set(true);
    this.authService
      .updateProfile({
        fullName: v.fullName!.trim(),
        contactNumber: v.contactNumber?.trim() || null,
        email: v.email?.trim() || null,
        address: v.address?.trim() || null,
        maritalStatus: (v.maritalStatus || null) as MaritalStatus | null,
        photo: this.photoChanged ? (this.profilePhoto() ?? '') : null,
      })
      .subscribe({
        next: () => {
          this.savingProfile.set(false);
          this.showProfile.set(false);
          this.toast.success('Profile updated.');
        },
        error: (err) => {
          this.savingProfile.set(false);
          this.toast.error(err.message);
        },
      });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
