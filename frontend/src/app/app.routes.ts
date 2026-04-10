import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component')
        .then(m => m.DashboardComponent),
  },
  {
    path: 'printers',
    loadComponent: () =>
      import('./features/printers/printer-list/printer-list.component')
        .then(m => m.PrinterListComponent),
  },
  {
    path: 'printers/add',
    loadComponent: () =>
      import('./features/printers/printer-form/printer-form.component')
        .then(m => m.PrinterFormComponent),
  },
  {
    path: 'printers/:ip/edit',
    loadComponent: () =>
      import('./features/printers/printer-form/printer-form.component')
        .then(m => m.PrinterFormComponent),
  },
  {
    path: 'printers/:ip',
    loadComponent: () =>
      import('./features/printers/printer-detail/printer-detail.component')
        .then(m => m.PrinterDetailComponent),
  },
  {
    path: 'discovery',
    loadComponent: () =>
      import('./features/discovery/discovery.component')
        .then(m => m.DiscoveryComponent),
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
