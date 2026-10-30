import { Routes } from '@angular/router';
import { Shell } from './layout/shell/shell';
import { Login } from './pages/login/login';
import { Dashboard } from './pages/dashboard/dashboard';
import { Barangays } from './pages/barangays/barangays';
import { Violators } from './pages/violators/violators';
import { Ordinances } from './pages/ordinances/ordinances';
import { Violations } from './pages/violations/violations';
import { FiledCases } from './pages/filed-cases/filed-cases';
import { Payments } from './pages/payments/payments';
import { SeminarRecords } from './pages/seminar-records/seminar-records';
import { CounselingRecords } from './pages/counseling-records/counseling-records';
import { CommunityServiceRecords } from './pages/community-service-records/community-service-records';
import { Users } from './pages/users/users';
import { Reports } from './pages/reports/reports';
import { System } from './pages/system/system';
import { Verify } from './pages/verify/verify';
import { authGuard, loggedInGuard } from './core/guard/auth-guard';
import { ChangePassword } from './pages/change-password/change-password';
import { roleGuard } from './core/guard/role-guard';
import { availableGuard, unavailablePageGuard } from './core/guard/availability-guard';
import { ServiceUnavailable } from './pages/service-unavailable/service-unavailable';

// Everyone except Apprehending Officers (who only get Dashboard, Violators, Ordinances and Violations).
const notOfficer = roleGuard('MISPersonnel', 'OVRSupervisor', 'PNPBarangayStaff');

export const routes: Routes = [
  { path: 'service-unavailable', component: ServiceUnavailable, canActivate: [unavailablePageGuard] },
  // Nothing below is reachable while environment.isAvailable is false.
  { path: 'login', component: Login, canMatch: [availableGuard] },
  { path: 'change-password', component: ChangePassword, canActivate: [loggedInGuard], canMatch: [availableGuard] },
  {
    path: '',
    component: Shell,
    canMatch: [availableGuard],
    canActivate: [authGuard],
    children: [
      { path: '', component: Dashboard },
      { path: 'barangays', component: Barangays, canActivate: [notOfficer] },
      { path: 'violators', component: Violators },
      { path: 'ordinances', component: Ordinances },
      { path: 'violations', component: Violations },
      { path: 'filed-cases', component: FiledCases, canActivate: [notOfficer] },
      { path: 'payments', component: Payments, canActivate: [notOfficer] },
      { path: 'seminar-records', component: SeminarRecords, canActivate: [notOfficer] },
      { path: 'counseling-records', component: CounselingRecords, canActivate: [notOfficer] },
      { path: 'community-service-records', component: CommunityServiceRecords, canActivate: [notOfficer] },
      { path: 'verify', component: Verify, canActivate: [notOfficer] },
      {
        path: 'reports',
        component: Reports,
        canActivate: [roleGuard('MISPersonnel', 'OVRSupervisor')],
      },
      {
        path: 'users',
        component: Users,
        canActivate: [roleGuard('MISPersonnel')],
      },
      {
        path: 'system',
        component: System,
        canActivate: [roleGuard('MISPersonnel')],
      },
    ],
  },
  { path: '**', canMatch: [availableGuard], redirectTo: '' },
];
