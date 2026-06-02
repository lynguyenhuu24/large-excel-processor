import { HttpClient, HttpEvent, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/paged-result.model';
import { InvoiceRecord } from '../models/excel-record.model';
import { FileRequest } from '../models/file-request.model';

@Injectable({ providedIn: 'root' })
export class ExcelService {
  private readonly apiUrl = '/api/excel';
  private readonly requestsUrl = '/api/filerequests';

  constructor(private http: HttpClient) {}

  upload(file: File): Observable<HttpEvent<FileRequest>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<FileRequest>(`${this.apiUrl}/upload`, formData, {
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
    pageSize: number = 50
  ): Observable<PagedResult<InvoiceRecord>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<InvoiceRecord>>(`${this.apiUrl}/records`, {
      params,
    });
  }

  deleteRequest(id: string): Observable<void> {
    return this.http.delete<void>(`${this.requestsUrl}/${id}`);
  }

  downloadSample(count: number): Observable<Blob> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get(`${this.apiUrl}/sample`, {
      params,
      responseType: 'blob',
    });
  }
}
