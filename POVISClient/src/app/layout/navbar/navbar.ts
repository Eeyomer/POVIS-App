import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth-service';
import { ViewModeService } from '../../core/services/view-mode-service';
import { SidebarService } from '../../core/services/sidebar-service';
import { MANILA_TZ, manilaHour } from '../../core/utils/manila-time';

@Component({
  selector: 'app-admin-navbar',
  imports: [CommonModule],
  templateUrl: './navbar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Navbar implements OnInit {
  username: string | null = null;
  greeting = 'Hello';
  today = '';

  constructor(
    public authService: AuthService,
    public viewMode: ViewModeService,
    private cdr: ChangeDetectorRef,
    public sidebarService: SidebarService,
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername();
    this.greeting = this.computeGreeting();
    this.today = new Date().toLocaleDateString('en-US', {
      timeZone: MANILA_TZ,
      weekday: 'long',
      month: 'long',
      day: 'numeric',
    });
    this.cdr.markForCheck();
  }

  private computeGreeting(): string {
    const hour = manilaHour();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }
}
