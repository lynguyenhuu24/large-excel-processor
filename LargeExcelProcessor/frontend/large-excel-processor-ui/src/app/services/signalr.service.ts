import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { FileRequest } from '../models/file-request.model';

export interface ProcessingNotification {
  jobId: string;
  status: string;
  totalRows?: number;
  importedRows?: number;
  errorMessage?: string;
}

export interface RequestStatusUpdate {
  jobId: string;
  status: string;
  totalRows?: number;
  importedRows?: number;
  fileSize?: number;
  resultBlobUri?: string;
  errorMessage?: string;
  completedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class SignalrService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private _notifications = new Subject<ProcessingNotification>();
  private _newRequests = new Subject<FileRequest>();
  private _requestsUpdates = new Subject<RequestStatusUpdate>();
  private _requestDeleted = new Subject<string>();

  notifications$ = this._notifications.asObservable();
  newRequests$ = this._newRequests.asObservable();
  requestsUpdates$ = this._requestsUpdates.asObservable();
  requestDeleted$ = this._requestDeleted.asObservable();

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

    this.hubConnection.on('NewRequest', (data: FileRequest) => {
      this._newRequests.next(data);
    });

    this.hubConnection.on('RequestStatusChanged', (data: RequestStatusUpdate) => {
      this._requestsUpdates.next(data);
    });

    this.hubConnection.on('RequestDeleted', (data: { id: string }) => {
      this._requestDeleted.next(data.id);
    });

    try {
      await this.hubConnection.start();
    } catch (err) {
      console.error('SignalR connection failed', err);
    }
  }

  async subscribeToJob(jobId: string): Promise<void> {
    await this.connect();
    try {
      await this.hubConnection?.invoke('SubscribeToJob', jobId);
    } catch (err) {
      console.error('SignalR subscribeToJob failed', err);
    }
  }

  async unsubscribeFromJob(jobId: string): Promise<void> {
    try {
      await this.hubConnection?.invoke('UnsubscribeFromJob', jobId);
    } catch {
    }
  }

  async subscribeToRequests(): Promise<void> {
    await this.connect();
    try {
      await this.hubConnection?.invoke('SubscribeToRequests');
    } catch (err) {
      console.error('SignalR subscribeToRequests failed', err);
    }
  }

  async unsubscribeFromRequests(): Promise<void> {
    try {
      await this.hubConnection?.invoke('UnsubscribeFromRequests');
    } catch {
    }
  }

  ngOnDestroy(): void {
    this._notifications.complete();
    this._newRequests.complete();
    this._requestsUpdates.complete();
    this._requestDeleted.complete();
    this.hubConnection?.stop();
  }
}
