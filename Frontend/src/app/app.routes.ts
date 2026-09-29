import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth.guard';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/landing.component').then((m) => m.LandingComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard.component').then((m) => m.DashboardComponent) },
      { path: 'my-meetings', loadComponent: () => import('./pages/my-meetings.component').then((m) => m.MyMeetingsComponent) },
      { path: 'meetings/new', loadComponent: () => import('./pages/meeting-form.component').then((m) => m.MeetingFormComponent) },
      { path: 'assistant', loadComponent: () => import('./pages/assistant.component').then((m) => m.AssistantComponent) },
      { path: 'unavailability', loadComponent: () => import('./pages/unavailability.component').then((m) => m.UnavailabilityComponent) },
      { path: 'minutes', loadComponent: () => import('./pages/minutes.component').then((m) => m.MinutesComponent) },
      { path: 'notifications', loadComponent: () => import('./pages/notifications.component').then((m) => m.NotificationsComponent) },
      { path: 'secretary', canActivate: [roleGuard('Admin', 'Secretaire')], loadComponent: () => import('./pages/secretary.component').then((m) => m.SecretaryComponent) },
      { path: 'rooms', canActivate: [roleGuard('Admin', 'Secretaire')], loadComponent: () => import('./pages/rooms.component').then((m) => m.RoomsComponent) },
      { path: 'statistics', canActivate: [roleGuard('Admin', 'Secretaire')], loadComponent: () => import('./pages/statistics.component').then((m) => m.StatisticsComponent) },
      { path: 'users', canActivate: [roleGuard('Admin')], loadComponent: () => import('./pages/users.component').then((m) => m.UsersComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
