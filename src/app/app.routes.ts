import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => {
      return import('./home/home.component').then((m) => m.HomeComponent)
    },
  },
  {
    path: 'add-stat',
    loadComponent: () => {
      return import('./add-stat/add-stat.component').then((m) => m.AddStatComponent)
    },
  }
];
