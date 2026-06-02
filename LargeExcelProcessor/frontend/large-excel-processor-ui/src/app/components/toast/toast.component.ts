import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject, OnDestroy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ToastService, ToastMessage } from '../../services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastComponent implements OnDestroy {
  message: ToastMessage | null = null;
  private timeoutId: ReturnType<typeof setTimeout> | null = null;
  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);

  constructor(private toastService: ToastService) {
    this.toastService.toasts$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((msg) => {
      this.message = msg;
      this.cdr.markForCheck();
      if (this.timeoutId) clearTimeout(this.timeoutId);
      this.timeoutId = setTimeout(() => {
        if (this.message?.id === msg.id) {
          this.message = null;
          this.cdr.markForCheck();
        }
        this.timeoutId = null;
      }, 5000);
    });
  }

  ngOnDestroy(): void {
    if (this.timeoutId) clearTimeout(this.timeoutId);
  }

  get marker(): string {
    if (!this.message) return '';
    switch (this.message.type) {
      case 'success': return '[+]';
      case 'error': return '[x]';
      case 'info': return '[*]';
    }
  }

  get markerClass(): string {
    if (!this.message) return '';
    switch (this.message.type) {
      case 'success': return 'marker-plus';
      case 'error': return 'marker-x';
      case 'info': return 'marker-bracket';
    }
  }
}
