import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: 'projects', loadComponent: () => import('./pages/projects').then((m) => m.ProjectsPage) },
  { path: 'results', loadComponent: () => import('./pages/results').then((m) => m.ResultsPage) },
  { path: '', pathMatch: 'full', redirectTo: 'projects' },
  { path: '**', redirectTo: 'projects' },
];
