import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { ExcelService } from '../../services/excel.service';
import { SignalrService, RequestStatusUpdate } from '../../services/signalr.service';
import { FileRequest } from '../../models/file-request.model';
import { SectionHeaderComponent } from '../../shared/section-header/section-header.component';
import { DataTableComponent } from '../../shared/data-table/data-table.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';
import { formatDuration as _formatDuration } from '../../shared/format-duration';

@Component({
  selector: 'app-requests',
  standalone: true,
  imports: [CommonModule, MatTableModule, SectionHeaderComponent, DataTableComponent, ConfirmDialogComponent],
  templateUrl: './requests.component.html',
  styleUrl: './requests.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsComponent {
  displayedColumns: string[] = [
    '#', 'id', 'requestType', 'fileName', 'fileSize',
    'status', 'importedRows', 'createdAt', 'duration', 'actions',
  ];
  dataSource = new MatTableDataSource<FileRequest>([]);
  totalCount = 0;
  loading = false;
  error: string | null = null;

  formatDuration = _formatDuration;
  confirmOpen = false;
  confirmDeleteId: string | null = null;

  private subs = new Subscription();
  private pendingRequests: FileRequest[] = [];
  currentPage = 1;
  currentPageSize = 50;
  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private excelService: ExcelService,
    private signalrService: SignalrService,
  ) {
    this.loadPage();
    this.subscribeToSignalR();
  }

  private subscribeToSignalR(): void {
    this.signalrService.connect().then(() => {
      this.signalrService.subscribeToRequests();
    });

    this.subs.add(
      this.signalrService.newRequests$.subscribe((req) => {
        this.pendingRequests.unshift(req);
        this.dataSource.data = [...this.pendingRequests, ...this.dataSource.data];
        this.totalCount++;
        this.cdr.markForCheck();
      }),
    );

    this.subs.add(
      this.signalrService.requestsUpdates$.subscribe((update) => {
        const idx = this.dataSource.data.findIndex(r => r.id === update.jobId);
        if (idx >= 0) {
          const updated = { ...this.dataSource.data[idx] };
          updated.status = update.status;
          if (update.totalRows != null) updated.totalRows = update.totalRows;
          if (update.importedRows != null) updated.importedRows = update.importedRows;
          if (update.fileSize != null) updated.fileSize = update.fileSize;
          if (update.resultBlobUri != null) updated.resultBlobUri = update.resultBlobUri;
          if (update.errorMessage != null) updated.errorMessage = update.errorMessage;
          if (update.completedAt) updated.completedAt = update.completedAt;
          const data = [...this.dataSource.data];
          data[idx] = updated;
          this.dataSource.data = data;
          this.cdr.markForCheck();
        }
      }),
    );

    this.subs.add(
      this.signalrService.requestDeleted$.subscribe((id) => {
        const idx = this.dataSource.data.findIndex(r => r.id === id);
        if (idx >= 0) {
          this.dataSource.data = this.dataSource.data.filter(r => r.id !== id);
          this.totalCount--;
          this.cdr.markForCheck();
        }
      }),
    );
  }

  onPageChange(evt: { page: number; pageSize: number }): void {
    this.currentPage = evt.page;
    this.currentPageSize = evt.pageSize;
    this.loadPage();
  }

  private loadPage(): void {
    this.loading = true;
    this.error = null;

    this.excelService.getFileRequests(this.currentPage, this.currentPageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.pendingRequests = [];
          this.dataSource.data = result.items;
          this.totalCount = result.totalCount;
          this.loading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.error = err.message ?? 'Failed to load requests.';
          this.loading = false;
          this.cdr.markForCheck();
        },
      });
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }

  shortId(id: string): string {
    return id.slice(0, 8);
  }

  downloadFile(id: string): void {
    this.excelService.downloadFile(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'download.xlsx';
          a.click();
          URL.revokeObjectURL(url);
        },
        error: () => {
          this.error = 'Download failed.';
          this.cdr.markForCheck();
        },
      });
  }

  promptDelete(id: string): void {
    this.confirmDeleteId = id;
    this.confirmOpen = true;
  }

  onDeleteConfirmed(): void {
    if (!this.confirmDeleteId) return;
    const id = this.confirmDeleteId;
    this.confirmDeleteId = null;
    this.excelService.deleteRequest(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loadPage();
        },
        error: (err) => {
          this.error = err.message ?? 'Delete failed.';
          this.cdr.markForCheck();
        },
      });
  }

  rowNumber(index: number): number {
    return (this.currentPage - 1) * this.currentPageSize + index + 1;
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.signalrService.unsubscribeFromRequests();
  }
}
