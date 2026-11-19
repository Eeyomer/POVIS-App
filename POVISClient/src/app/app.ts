import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { StorageNoticeService } from './core/services/storage-notice-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  storageNotice = inject(StorageNoticeService);
}
