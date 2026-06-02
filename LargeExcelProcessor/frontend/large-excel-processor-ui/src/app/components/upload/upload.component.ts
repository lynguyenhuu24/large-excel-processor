import { Component, OnDestroy } from '@angular/core';
import { HttpEventType } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { ExcelService } from '../../services/excel.service';
import { ToastService } from '../../services/toast.service';
import { SignalrService } from '../../services/signalr.service';
import { SectionHeaderComponent } from '../../shared/section-header/section-header.component';
import { StatusBannerComponent } from '../../shared/status-banner/status-banner.component';
import { ProgressBarComponent } from '../../shared/progress-bar/progress-bar.component';
import { formatElapsed } from '../../shared/format-duration';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule, SectionHeaderComponent, StatusBannerComponent, ProgressBarComponent],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.scss',
})
export class UploadComponent implements OnDestroy {
  selectedFile: File | null = null;
  uploading = false;
  uploaded = false;
  progress = 0;
  error: string | null = null;
  dragOver = false;
  processingStatus: string | null = null;
  processingProgress = 0;
  processingTotalRows = 0;
  processingImportedRows = 0;
  private clickTime = 0;
  private sub: Subscription | null = null;

  constructor(
    private excelService: ExcelService,
    private toastService: ToastService,
    private signalrService: SignalrService
  ) {}

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = true;
  }

  onDragLeave(): void {
    this.dragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
    const file = event.dataTransfer?.files[0];
    if (file) this.selectFile(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.selectFile(input.files[0]);
  }

  private selectFile(file: File): void {
    if (!file.name.endsWith('.xlsx')) {
      this.error = 'Only .xlsx files are supported.';
      this.selectedFile = null;
      return;
    }
    this.selectedFile = file;
    this.error = null;
    this.progress = 0;
    this.uploaded = false;
    this.processingStatus = null;
  }

  upload(): void {
    if (!this.selectedFile) return;

    this.uploading = true;
    this.error = null;
    this.uploaded = false;
    this.processingStatus = null;
    this.clickTime = Date.now();

    this.excelService.upload(this.selectedFile).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          this.progress = Math.round((100 * event.loaded) / event.total);
        } else if (event.type === HttpEventType.Response && event.body) {
          this.uploading = false;
          this.uploaded = true;
          const jobId = event.body.id;
          this.toastService.show('file queued for processing', 'info');
          this.subscribeToJob(jobId);
        }
      },
      error: (err) => {
        this.error = err.message ?? 'Upload failed.';
        this.uploading = false;
      },
    });
  }

  private async subscribeToJob(jobId: string): Promise<void> {
    this.sub?.unsubscribe();
    this.processingProgress = 0;
    this.processingTotalRows = 0;
    this.processingImportedRows = 0;
    this.sub = this.signalrService.notifications$.subscribe((n) => {
      if (n.jobId === jobId) {
        const elapsed = this.clickTime ? ` in ${formatElapsed(this.clickTime)}` : '';
        if (n.status === 'Processing' && n.totalRows && n.totalRows > 0) {
          this.processingTotalRows = n.totalRows;
          this.processingImportedRows = n.importedRows ?? 0;
          this.processingProgress = Math.round((this.processingImportedRows / n.totalRows) * 100);
        } else if (n.status === 'Completed') {
          this.processingProgress = 100;
          this.processingStatus = `completed — ${n.importedRows} rows imported${elapsed}`;
          this.toastService.show(
            `processing complete — ${n.importedRows} rows imported${elapsed}`,
            'success'
          );
        } else if (n.status === 'Failed') {
          this.processingStatus = `failed: ${n.errorMessage}${elapsed}`;
          this.toastService.show(
            `processing failed: ${n.errorMessage}${elapsed}`,
            'error'
          );
        }
      }
    });

    try {
      await this.signalrService.subscribeToJob(jobId);
    } catch {
      this.toastService.show('failed to connect to real-time updates', 'error');
    }
  }

  reset(): void {
    this.selectedFile = null;
    this.uploading = false;
    this.uploaded = false;
    this.progress = 0;
    this.error = null;
    this.processingStatus = null;
    this.processingProgress = 0;
    this.processingTotalRows = 0;
    this.processingImportedRows = 0;
    this.clickTime = 0;
    if (this.sub) { this.sub.unsubscribe(); this.sub = null; }
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
