import { Component, model, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-filter-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="filter-bar">
      <span class="filter-label">{{ searchLabel() }}</span>
      <input
        class="filter-input"
        type="text"
        [(ngModel)]="search"
        [placeholder]="searchPlaceholder()"
        (keydown.enter)="apply.emit()"
      />
      @if (showStatus()) {
        <span class="filter-label">{{ statusLabel() }}</span>
        <select class="filter-select" [(ngModel)]="status">
          <option value="">all statuses</option>
          @for (s of statuses(); track s) {
            <option [value]="s">{{ s }}</option>
          }
        </select>
      }
      @if (showDates()) {
        <span class="filter-label">{{ dateFromLabel() }}</span>
        <input class="filter-input filter-date" type="date" [(ngModel)]="dateFrom" />
        <span class="filter-label">{{ dateToLabel() }}</span>
        <input class="filter-input filter-date" type="date" [(ngModel)]="dateTo" />
      }
      <button class="btn btn-sm" (click)="apply.emit()">apply</button>
      <button class="btn btn-sm" (click)="resetFilters()">reset</button>
    </div>
  `,
  styles: [`
    .filter-bar {
      display: flex;
      gap: 8px;
      align-items: center;
      flex-wrap: wrap;
      margin-bottom: 16px;
    }
    .filter-input {
      font-family: var(--font-family);
      font-size: 14px;
      padding: 0 10px;
      height: 32px;
      border: 1px solid var(--hairline-strong);
      border-radius: 4px;
      background: var(--canvas);
      color: var(--ink);
      outline: none;
      min-width: 200px;
    }
    .filter-input:focus {
      border-color: var(--ink);
    }
    .filter-date {
      min-width: 140px;
    }
    .filter-select {
      font-family: var(--font-family);
      font-size: 14px;
      padding: 0 8px;
      height: 32px;
      border: 1px solid var(--hairline-strong);
      border-radius: 4px;
      background: var(--canvas);
      color: var(--ink);
      outline: none;
      min-width: 130px;
    }
    .filter-select:focus {
      border-color: var(--ink);
    }
    .filter-label {
      color: var(--mute);
      font-size: 14px;
      white-space: nowrap;
    }
  `],
})
export class FilterBarComponent {
  search = model('');
  status = model('');
  dateFrom = model('');
  dateTo = model('');
  statuses = input<string[]>([]);
  showStatus = input(true);
  showDates = input(true);
  searchPlaceholder = input('search...');
  searchLabel = input('search');
  statusLabel = input('status');
  dateFromLabel = input('from');
  dateToLabel = input('to');
  apply = output<void>();
  reset = output<void>();

  resetFilters(): void {
    this.search.set('');
    this.status.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
    this.reset.emit();
  }
}
