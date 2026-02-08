import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register/register.page').then(m => m.RegisterPage),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/dashboard/dashboard.page').then(m => m.DashboardPage),
  },
  {
    path: 'browse',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/browse/browse.page').then(m => m.BrowsePage),
  },
  {
    path: 'browse/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/profile-detail/profile-detail.page').then(m => m.ProfileDetailPage),
  },
  {
    path: 'profiles/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/profile-editor/profile-editor.page').then(m => m.ProfileEditorPage),
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
