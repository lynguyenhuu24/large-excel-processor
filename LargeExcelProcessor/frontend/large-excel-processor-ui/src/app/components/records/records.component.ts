import { Component, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { ExcelService } from '../../services/excel.service';
import { InvoiceRecord } from '../../models/excel-record.model';

@Component({
  selector: 'app-records',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatPaginatorModule],
  templateUrl: './records.component.html',
  styleUrl: './records.component.scss',
})
export class RecordsComponent implements AfterViewInit {
  displayedColumns: string[] = [
    'id', 'invoiceNumber', 'vendorName', 'customerName',
    'totalAmount', 'currencyCode', 'status', 'dueDate',
  ];
  dataSource = new MatTableDataSource<InvoiceRecord>([]);
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

    this.excelService.getRecords(page, pageSize).subscribe({
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
}
