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
import { authGuard } from './core/guard/auth-guard';
import { roleGuard } from './core/guard/role-guard';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: 'admin',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', component: Dashboard },
      { path: 'barangays', component: Barangays },
      { path: 'violators', component: Violators },
      { path: 'ordinances', component: Ordinances },
      { path: 'violations', component: Violations },
      { path: 'filed-cases', component: FiledCases },
      { path: 'payments', component: Payments },
      { path: 'seminar-records', component: SeminarRecords },
      { path: 'counseling-records', component: CounselingRecords },
      { path: 'community-service-records', component: CommunityServiceRecords },
      {
        path: 'users',
        component: Users,
        canActivate: [roleGuard('MISPersonnel')],
      },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'admin' },
  { path: '**', redirectTo: 'admin' },
];
