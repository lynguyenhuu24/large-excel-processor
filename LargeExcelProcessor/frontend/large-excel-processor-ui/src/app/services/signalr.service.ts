import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

export interface ProcessingNotification {
  jobId: string;
  status: string;
  totalRows?: number;
  importedRows?: number;
  errorMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class SignalrService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private _notifications = new Subject<ProcessingNotification>();
  notifications$ = this._notifications.asObservable();

  async connect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected)
      return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/upload')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ProcessingCompleted', (data: ProcessingNotification) => {
      this._notifications.next(data);
    });

    await this.hubConnection.start();
  }

  async subscribeToJob(jobId: string): Promise<void> {
    await this.connect();
    await this.hubConnection?.invoke('SubscribeToJob', jobId);
  }

  async unsubscribeFromJob(jobId: string): Promise<void> {
    await this.hubConnection?.invoke('UnsubscribeFromJob', jobId);
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop();
  }
}
