import { ChangeDetectionStrategy, Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-progress-bar',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="progress-section">
      <p class="progress-bar">{{ bar() }}</p>
      <p class="progress-text">{{ value() }}% {{ label() }}</p>
    </div>
  `,
  styles: [`
    .progress-section {
      margin-top: 16px;
      text-align: center;
    }
    .progress-bar {
      font-family: var(--font-family);
      font-size: 16px;
      color: var(--ink);
      letter-spacing: 1px;
      margin-bottom: 4px;
    }
    .progress-text {
      font-size: 14px;
      color: var(--mute);
      line-height: 2;
    }
  `],
})
export class ProgressBarComponent {
  value = input(0);
  label = input('uploaded');

  bar = computed(() => {
    const width = 30;
    const filled = Math.round((this.value() / 100) * width);
    return '[' + '#'.repeat(filled) + '\u00b7'.repeat(width - filled) + ']';
  });
}
