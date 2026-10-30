import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-service-unavailable',
  templateUrl: './service-unavailable.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceUnavailable {
  reload() {
    location.assign('/');
  }
}
