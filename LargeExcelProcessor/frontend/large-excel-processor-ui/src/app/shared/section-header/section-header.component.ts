import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-section-header',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="section-header">
      <span>{{ title() }}</span>
      @if (showAction()) {
        <button
          class="btn btn-sm"
          [class.btn-primary]="actionPrimary()"
          (click)="actionClick.emit()"
          [disabled]="actionDisabled() || actionBusy()"
        >
          @if (actionBusy()) {
            [*] {{ actionBusyLabel() }}&hellip;
          } @else {
            {{ actionLabel() }}
          }
        </button>
      }
    </div>
  `,
  styles: [`
    .section-header {
      font-size: 16px;
      font-weight: 700;
      line-height: 1.5;
      color: var(--ink);
      padding-bottom: 12px;
      border-bottom: 1px solid var(--hairline);
      margin-bottom: 16px;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
  `],
})
export class SectionHeaderComponent {
  title = input.required<string>();
  showAction = input(false);
  actionLabel = input('');
  actionBusyLabel = input('');
  actionBusy = input(false);
  actionDisabled = input(false);
  actionPrimary = input(false);
  actionClick = output<void>();
}
