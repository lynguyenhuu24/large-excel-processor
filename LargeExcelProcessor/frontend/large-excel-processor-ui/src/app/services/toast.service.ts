import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface ToastMessage {
  text: string;
  type: 'success' | 'error' | 'info';
  id: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts = new Subject<ToastMessage>();
  toasts$ = this._toasts.asObservable();
  private counter = 0;

  show(text: string, type: 'success' | 'error' | 'info' = 'info'): void {
    this._toasts.next({ text, type, id: ++this.counter });
  }
}
