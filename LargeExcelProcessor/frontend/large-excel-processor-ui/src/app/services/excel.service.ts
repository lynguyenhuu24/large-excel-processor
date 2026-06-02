import { HttpClient, HttpEvent, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/paged-result.model';
import { InvoiceRecord } from '../models/excel-record.model';
import { FileRequest } from '../models/file-request.model';

@Injectable({ providedIn: 'root' })
export class ExcelService {
  private readonly uploadUrl = '/api/upload';
  private readonly recordsUrl = '/api/records';
  private readonly exportUrl = '/api/export';
  private readonly sampleUrl = '/api/sample';
  private readonly requestsUrl = '/api/filerequests';

  constructor(private http: HttpClient) {}

  upload(file: File): Observable<HttpEvent<FileRequest>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<FileRequest>(this.uploadUrl, formData, {
      reportProgress: true,
      observe: 'events',
    });
  }

  getFileRequests(
    page: number = 1,
    pageSize: number = 50
  ): Observable<PagedResult<FileRequest>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<FileRequest>>(this.requestsUrl, { params });
  }

  getRecords(
    page: number = 1,
    pageSize: number = 50,
    search?: string,
    status?: string,
    dateFrom?: string,
    dateTo?: string
  ): Observable<PagedResult<InvoiceRecord>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    if (dateFrom) params = params.set('dateFrom', dateFrom);
    if (dateTo) params = params.set('dateTo', dateTo);
    return this.http.get<PagedResult<InvoiceRecord>>(this.recordsUrl, {
      params,
    });
  }

  exportRecords(
    search?: string,
    status?: string,
    dateFrom?: string,
    dateTo?: string
  ): Observable<FileRequest> {
    return this.http.post<FileRequest>(this.exportUrl, {
      search,
      status,
      dateFrom,
      dateTo,
    });
  }

  deleteRequest(id: string): Observable<void> {
    return this.http.delete<void>(`${this.requestsUrl}/${id}`);
  }

  downloadSample(count: number): Observable<Blob> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get(this.sampleUrl, {
      params,
      responseType: 'blob',
    });
  }

  downloadFile(id: string): Observable<Blob> {
    return this.http.get(`${this.requestsUrl}/${id}/download`, {
      responseType: 'blob',
    });
  }
}
