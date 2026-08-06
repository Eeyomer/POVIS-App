import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../sidebar/sidebar';
import { Navbar } from '../navbar/navbar';
import { SidebarService } from '../../core/services/sidebar-service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, Sidebar, Navbar],
  templateUrl: './shell.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  constructor(public sidebarService: SidebarService) {}
}
