import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, throwError } from 'rxjs';
import { IncentiveFilter, IncentiveList } from '../models/app-dto';
import { IncentivesApi } from './api-service/incentives-api';

@Injectable({ providedIn: 'root' })
export class IncentivesService {
  constructor(private api: IncentivesApi) {}

  list(filter: IncentiveFilter, mine: boolean): Observable<IncentiveList> {
    return this.api.get(filter, mine).pipe(
      map((res) => res.data as IncentiveList),
      catchError((err: HttpErrorResponse) =>
        throwError(() => new Error(err?.error?.message || 'Could not load incentives. Please try again.')),
      ),
    );
  }
}
