import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { isAvailable } from '../environment/availability';

/** Lets every route through only while the system is flagged as available. */
export const availableGuard: CanMatchFn = () => {
  return isAvailable ? true : inject(Router).createUrlTree(['/service-unavailable']);
};

/** The error page itself: when the system is available, send visitors home. */
export const unavailablePageGuard: CanActivateFn = () => {
  return isAvailable ? inject(Router).createUrlTree(['/']) : true;
};
