import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatPaginatorModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <p class="loading-text">loading...</p>
    }

    @if (error()) {
      <p class="error-text">
        <span class="marker-bracket">[</span><span class="marker-x">x</span><span class="marker-bracket">]</span> {{ error() }}
      </p>
    }

    @if (!loading() && !error()) {
      <p class="result-count">{{ totalCount() }} record{{ totalCount() === 1 ? '' : 's' }}</p>

      <div class="table-wrapper">
        <ng-content />
      </div>

      <mat-paginator
        [length]="totalCount()"
        [pageSize]="pageSize()"
        [pageIndex]="pageIndex()"
        [pageSizeOptions]="pageSizeOptions()"
        showFirstLastButtons
        (page)="onPageChange($event)"
      ></mat-paginator>
    }
  `,
  styles: [`
    .loading-text {
      font-family: var(--font-family);
      color: var(--mute);
      text-align: center;
      padding: 48px 0;
    }
    .error-text {
      font-family: var(--font-family);
      color: var(--danger);
      text-align: center;
      padding: 24px 0;
    }
    .result-count {
      color: var(--mute);
      font-size: 14px;
      margin-bottom: 12px;
    }
    .table-wrapper {
      overflow-x: auto;
    }
  `],
})
export class DataTableComponent {
  loading = input(false);
  error = input<string | null>(null);
  totalCount = input(0);
  pageSize = input(50);
  pageSizeOptions = input([25, 50, 100]);
  pageIndex = input(0);
  pageChange = output<{ page: number; pageSize: number }>();

  onPageChange(evt: PageEvent): void {
    this.pageChange.emit({ page: evt.pageIndex + 1, pageSize: evt.pageSize });
  }
}
