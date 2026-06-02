import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { ExcelService } from '../../services/excel.service';
import { ToastService } from '../../services/toast.service';
import { SignalrService } from '../../services/signalr.service';
import { InvoiceRecord } from '../../models/excel-record.model';
import { SectionHeaderComponent } from '../../shared/section-header/section-header.component';
import { DataTableComponent } from '../../shared/data-table/data-table.component';
import { FilterBarComponent } from '../../shared/filter-bar/filter-bar.component';
import { ProgressBarComponent } from '../../shared/progress-bar/progress-bar.component';
import { formatElapsed } from '../../shared/format-duration';

@Component({
  selector: 'app-records',
  standalone: true,
  imports: [CommonModule, MatTableModule, SectionHeaderComponent, DataTableComponent, FilterBarComponent, ProgressBarComponent],
  templateUrl: './records.component.html',
  styleUrl: './records.component.scss',
})
export class RecordsComponent implements OnInit, OnDestroy {
  displayedColumns: string[] = [
    '#', 'id', 'invoiceNumber', 'vendorName', 'customerName',
    'totalAmount', 'currencyCode', 'status', 'dueDate',
  ];
  dataSource = new MatTableDataSource<InvoiceRecord>([]);
  totalCount = 0;
  loading = false;
  error: string | null = null;

  search = '';
  statusFilter = '';
  dateFrom = '';
  dateTo = '';
  exporting = false;
  exportProgress = 0;
  exportTotalRows = 0;
  exportImportedRows = 0;

  currentPage = 1;
  currentPageSize = 50;
  private exportClickTime = 0;
  private exportSub: Subscription | null = null;

  constructor(
    private excelService: ExcelService,
    private toastService: ToastService,
    private signalrService: SignalrService,
  ) {}

  ngOnInit(): void {
    this.loadPage();
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadPage();
  }

  resetFilters(): void {
    this.search = '';
    this.statusFilter = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.currentPage = 1;
    this.loadPage();
  }

  onPageChange(evt: { page: number; pageSize: number }): void {
    this.currentPage = evt.page;
    this.currentPageSize = evt.pageSize;
    this.loadPage();
  }

  private loadPage(): void {
    this.loading = true;
    this.error = null;

    this.excelService
      .getRecords(
        this.currentPage,
        this.currentPageSize,
        this.search || undefined,
        this.statusFilter || undefined,
        this.dateFrom || undefined,
        this.dateTo || undefined
      )
      .subscribe({
        next: (result) => {
          this.dataSource.data = result.items;
          this.totalCount = result.totalCount;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.message ?? 'Failed to load records.';
          this.loading = false;
        },
      });
  }

  exportRecords(): void {
    this.exporting = true;
    this.exportProgress = 0;
    this.exportTotalRows = 0;
    this.exportImportedRows = 0;
    this.exportClickTime = Date.now();
    this.excelService
      .exportRecords(
        this.search || undefined,
        this.statusFilter || undefined,
        this.dateFrom || undefined,
        this.dateTo || undefined
      )
      .subscribe({
        next: (result) => {
          this.exporting = false;
          this.toastService.show('export queued', 'info');
          this.listenForExportCompletion(result.id);
        },
        error: () => {
          this.exporting = false;
        },
      });
  }

  private listenForExportCompletion(jobId: string): void {
    this.signalrService.connect().then(() => {
      this.exportSub?.unsubscribe();
      this.exportSub = this.signalrService.notifications$.subscribe((n) => {
        if (n.jobId === jobId) {
          const elapsed = this.exportClickTime ? ` in ${formatElapsed(this.exportClickTime)}` : '';
          if (n.status === 'Processing' && n.totalRows && n.totalRows > 0) {
            this.exportTotalRows = n.totalRows;
            this.exportImportedRows = n.importedRows ?? 0;
            this.exportProgress = Math.round((this.exportImportedRows / n.totalRows) * 100);
          } else if (n.status === 'Completed') {
            this.exportProgress = 100;
            this.exporting = false;
            this.toastService.show(`export completed — ${n.importedRows} rows${elapsed}`, 'success');
          } else if (n.status === 'Failed') {
            this.exporting = false;
            this.toastService.show(`export failed: ${n.errorMessage}${elapsed}`, 'error');
          }
        }
      });
    });
  }

  ngOnDestroy(): void {
    this.exportSub?.unsubscribe();
  }
}
