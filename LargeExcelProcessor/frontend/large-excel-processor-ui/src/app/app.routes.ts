import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/requests', pathMatch: 'full' },
  { path: 'upload', loadComponent: () => import('./components/upload/upload.component').then(m => m.UploadComponent), title: 'Upload Excel' },
  { path: 'records', loadComponent: () => import('./components/records/records.component').then(m => m.RecordsComponent), title: 'View Records' },
  { path: 'requests', loadComponent: () => import('./components/requests/requests.component').then(m => m.RequestsComponent), title: 'File Requests' },
  { path: 'sample', loadComponent: () => import('./components/sample/sample.component').then(m => m.SampleComponent), title: 'Sample Generator' },
];
