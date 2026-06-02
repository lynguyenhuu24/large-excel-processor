import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { ToastService, ToastMessage } from '../../services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss',
})
export class ToastComponent implements OnDestroy {
  message: ToastMessage | null = null;
  private sub: Subscription;

  constructor(private toastService: ToastService) {
    this.sub = this.toastService.toasts$.subscribe((msg) => {
      this.message = msg;
      setTimeout(() => {
        if (this.message?.id === msg.id) this.message = null;
      }, 5000);
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
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
