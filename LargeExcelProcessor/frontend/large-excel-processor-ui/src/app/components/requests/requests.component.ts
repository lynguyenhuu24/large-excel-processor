import { Component, OnInit, OnDestroy } from '@angular/core';
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
})
export class RequestsComponent implements OnInit, OnDestroy {
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

  constructor(
    private excelService: ExcelService,
    private signalrService: SignalrService,
  ) {}

  ngOnInit(): void {
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
      }),
    );

    this.subs.add(
      this.signalrService.requestsUpdates$.subscribe((update) => {
        const idx = this.dataSource.data.findIndex(r => r.id === update.jobId);
        if (idx >= 0) {
          const row = this.dataSource.data[idx];
          row.status = update.status;
          if (update.totalRows != null) row.totalRows = update.totalRows;
          if (update.importedRows != null) row.importedRows = update.importedRows;
          if (update.fileSize != null) row.fileSize = update.fileSize;
          if (update.resultBlobUri != null) row.resultBlobUri = update.resultBlobUri;
          if (update.errorMessage != null) row.errorMessage = update.errorMessage;
          if (update.completedAt) row.completedAt = update.completedAt;
          this.dataSource.data = [...this.dataSource.data];
        }
      }),
    );

    this.subs.add(
      this.signalrService.requestDeleted$.subscribe((id) => {
        const idx = this.dataSource.data.findIndex(r => r.id === id);
        if (idx >= 0) {
          this.dataSource.data = this.dataSource.data.filter(r => r.id !== id);
          this.totalCount--;
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

    this.excelService.getFileRequests(this.currentPage, this.currentPageSize).subscribe({
      next: (result) => {
        this.pendingRequests = [];
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message ?? 'Failed to load requests.';
        this.loading = false;
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
    window.open(`/api/filerequests/${id}/download`, '_blank');
  }

  promptDelete(id: string): void {
    this.confirmDeleteId = id;
    this.confirmOpen = true;
  }

  onDeleteConfirmed(): void {
    if (!this.confirmDeleteId) return;
    const id = this.confirmDeleteId;
    this.confirmDeleteId = null;
    this.excelService.deleteRequest(id).subscribe({
      next: () => this.loadPage(),
      error: (err) => { this.error = err.message ?? 'Delete failed.'; },
    });
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.signalrService.unsubscribeFromRequests();
  }
}
