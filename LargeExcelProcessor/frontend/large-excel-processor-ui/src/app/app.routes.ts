import { Routes } from '@angular/router';
import { UploadComponent } from './components/upload/upload.component';
import { RecordsComponent } from './components/records/records.component';
import { RequestsComponent } from './components/requests/requests.component';
import { SampleComponent } from './components/sample/sample.component';

export const routes: Routes = [
  { path: '', redirectTo: '/requests', pathMatch: 'full' },
  { path: 'upload', component: UploadComponent, title: 'Upload Excel' },
  { path: 'records', component: RecordsComponent, title: 'View Records' },
  { path: 'requests', component: RequestsComponent, title: 'File Requests' },
  { path: 'sample', component: SampleComponent, title: 'Sample Generator' },
];
