import { Routes } from '@angular/router';
import { HomeComponent } from './components/home.component';
import { BookingComponent } from './components/booking.component';
import { HistoryComponent } from './components/history.component';
import { ProfileComponent } from './components/profile.component';
import { LoginComponent } from './components/login.component';
import { RegisterComponent } from './components/register.component';
import { DashboardComponent } from './components/dashboard.component';
import { UnauthorizedComponent } from './components/unauthorized.component';
import { AuthGuard } from './guards/auth.guard';
import { OperatorLoginComponent } from './components/operator-login.component';
import { OperatorDashboardComponent } from './components/operator-dashboard.component';
import { AdminLoginComponent } from './components/admin-login.component';
import { AdminDashboardComponent } from './components/admin-dashboard.component';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'book/:tripId',
    component: BookingComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'history',
    component: HistoryComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'profile',
    component: ProfileComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'unauthorized',
    component: UnauthorizedComponent
  },
  {
    path: 'operator/login',
    component: OperatorLoginComponent
  },
  {
    path: 'operator/dashboard',
    component: OperatorDashboardComponent,
    canActivate: [AuthGuard],
    data: { roles: ['Operator'] }
  },
  {
    path: 'admin/login',
    component: AdminLoginComponent
  },
  {
    path: 'admin/dashboard',
    component: AdminDashboardComponent,
    canActivate: [AuthGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: '**',
    redirectTo: '/'
  }
];
