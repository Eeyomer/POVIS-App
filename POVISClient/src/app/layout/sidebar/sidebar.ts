import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
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
  'seminar-records': 'compliance',
  'counseling-records': 'compliance',
  'community-service-records': 'compliance',
  reports: 'reports',
  users: 'administration',
  system: 'administration',
};

@Component({
  selector: 'app-admin-sidebar',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sidebar implements OnInit {
  username: string | null = null;
  initials = 'A';
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
    this.initials = this.computeInitials(this.username);
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

  private computeInitials(username: string | null): string {
    if (!username) return 'A';
    return username.trim().slice(0, 2).toUpperCase();
  }

  onNavigate(): void {
    this.sidebarService.close();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
