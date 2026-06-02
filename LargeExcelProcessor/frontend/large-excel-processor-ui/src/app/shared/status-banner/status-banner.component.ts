import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-status-banner',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (message()) {
      <div class="result-block" [class.result-error]="type() === 'error'" [class.result-success]="type() === 'success'">
        <p>
          <span class="marker-bracket">[</span>
          <span [class.marker-plus]="type() === 'success'" [class.marker-x]="type() === 'error'" [class.marker-bracket]="type() === 'info'">
            {{ type() === 'success' ? '+' : type() === 'error' ? 'x' : '*' }}
          </span>
          <span class="marker-bracket">]</span>
          {{ message() }}
        </p>
      </div>
    }
  `,
  styles: [`
    .result-block {
      margin-top: 16px;
      padding: 12px 16px;
      border: 1px solid var(--hairline);
    }
    .result-error {
      border-color: var(--danger);
    }
    .result-success {
      border-color: var(--hairline);
    }
  `],
})
export class StatusBannerComponent {
  message = input<string | null>(null);
  type = input<'error' | 'success' | 'info'>('info');
}
