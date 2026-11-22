import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { APIResponse } from '../../models/app-model';
import { IncentiveFilter, IncentiveList } from '../../models/app-dto';

@Injectable({ providedIn: 'root' })
export class IncentivesApi {
  private apiUrl = `${environment.apiUrl}/api/Incentives`;

  constructor(private http: HttpClient) {}

  /** @param mine true for the signed-in Apprehending Officer's own incentives. */
  get(filter: IncentiveFilter, mine: boolean): Observable<APIResponse<IncentiveList>> {
    let params = new HttpParams().set('period', filter.period);
    if (filter.period === 'custom') {
      if (filter.from) params = params.set('from', filter.from);
      if (filter.to) params = params.set('to', filter.to);
    } else if (filter.date) {
      params = params.set('date', filter.date);
    }
    if (!mine && filter.officerId) params = params.set('officerId', filter.officerId);
    if (filter.division) params = params.set('division', filter.division);
    if (filter.search) params = params.set('search', filter.search);
    params = params.set('page', filter.page ?? 1).set('pageSize', filter.pageSize ?? 20);
    return this.http.get<APIResponse<IncentiveList>>(mine ? `${this.apiUrl}/mine` : this.apiUrl, { params });
  }
}
