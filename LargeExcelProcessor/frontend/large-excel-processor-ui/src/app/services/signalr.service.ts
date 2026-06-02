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

    await this.hubConnection.start();
  }

  async subscribeToJob(jobId: string): Promise<void> {
    await this.connect();
    await this.hubConnection?.invoke('SubscribeToJob', jobId);
  }

  async unsubscribeFromJob(jobId: string): Promise<void> {
    await this.hubConnection?.invoke('UnsubscribeFromJob', jobId);
  }

  async subscribeToRequests(): Promise<void> {
    await this.connect();
    await this.hubConnection?.invoke('SubscribeToRequests');
  }

  async unsubscribeFromRequests(): Promise<void> {
    await this.hubConnection?.invoke('UnsubscribeFromRequests');
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop();
  }
}
