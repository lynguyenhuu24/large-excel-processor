import { Component, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { ExcelService } from '../../services/excel.service';
import { FileRequest } from '../../models/file-request.model';

@Component({
  selector: 'app-requests',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatPaginatorModule],
  templateUrl: './requests.component.html',
  styleUrl: './requests.component.scss',
})
export class RequestsComponent implements AfterViewInit {
  displayedColumns: string[] = [
    'id', 'requestType', 'fileName', 'fileSize',
    'status', 'importedRows', 'createdAt', 'actions',
  ];
  dataSource = new MatTableDataSource<FileRequest>([]);
  totalCount = 0;
  loading = false;
  error: string | null = null;

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(private excelService: ExcelService) {}

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.loadPage();
    this.paginator.page.subscribe(() => this.loadPage());
  }

  private loadPage(): void {
    if (!this.paginator) return;

    this.loading = true;
    this.error = null;
    const page = this.paginator.pageIndex + 1;
    const pageSize = this.paginator.pageSize;

    this.excelService.getFileRequests(page, pageSize).subscribe({
      next: (result) => {
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

  downloadBlob(blobUri: string | undefined): void {
    if (blobUri) window.open(blobUri, '_blank');
  }

  deleteRequest(id: string): void {
    if (!confirm('delete this request and all imported data?')) return;
    this.excelService.deleteRequest(id).subscribe({
      next: () => this.loadPage(),
      error: (err) => { this.error = err.message ?? 'Delete failed.'; },
    });
  }
}
