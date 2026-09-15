import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth-service';
import { SidebarService } from '../../core/services/sidebar-service';

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
