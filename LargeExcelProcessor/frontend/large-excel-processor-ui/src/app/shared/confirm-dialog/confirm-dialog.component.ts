import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (open()) {
      <div class="overlay" (click)="dismiss()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <p>{{ message() }}</p>
          <div class="actions">
            <button class="btn btn-sm" (click)="dismiss()">cancel</button>
            <button class="btn btn-sm btn-primary" (click)="confirm()">delete</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .overlay {
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.15);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 999;
    }
    .dialog {
      background: var(--canvas);
      border: 1px solid var(--hairline-strong);
      padding: 24px;
      min-width: 320px;
    }
    .dialog p {
      font-family: var(--font-family);
      color: var(--ink);
      margin-bottom: 20px;
      line-height: 1.6;
    }
    .actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
    }
  `],
})
export class ConfirmDialogComponent {
  open = model(false);
  message = input('Are you sure?');
  confirmed = output<void>();

  confirm(): void {
    this.confirmed.emit();
    this.open.set(false);
  }

  dismiss(): void {
    this.open.set(false);
  }
}
