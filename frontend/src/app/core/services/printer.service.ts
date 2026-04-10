import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  Printer, PrinterStats, PrinterListResponse,
  UsageDto, DiscoveryResult,
  AddPrinterRequest, UpdatePrinterRequest
} from '../models/printer.model';

@Injectable({ providedIn: 'root' })
export class PrinterService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // Printers

  getAll(): Observable<Printer[]> {
    return this.http
      .get<PrinterListResponse>(`${this.base}/printers`)
      .pipe(map(r => r.printers));
  }

  getByIp(ip: string): Observable<Printer> {
    return this.http.get<Printer>(`${this.base}/printers/${encodeURIComponent(ip)}`);
  }

  getStats(): Observable<PrinterStats> {
    return this.http.get<PrinterStats>(`${this.base}/printers/stats`);
  }

  getUsage(ip: string): Observable<UsageDto> {
    return this.http.get<UsageDto>(`${this.base}/printers/${encodeURIComponent(ip)}/usage`);
  }

  add(request: AddPrinterRequest): Observable<Printer> {
    return this.http.post<Printer>(`${this.base}/printers`, request);
  }

  update(ip: string, request: UpdatePrinterRequest): Observable<Printer> {
    return this.http.put<Printer>(`${this.base}/printers/${encodeURIComponent(ip)}`, request);
  }

  delete(ip: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/printers/${encodeURIComponent(ip)}`);
  }

  poll(ip: string): Observable<Printer> {
    return this.http.post<Printer>(`${this.base}/printers/${encodeURIComponent(ip)}/poll`, {});
  }

  pollAll(): Observable<{ total: number; succeeded: number; failed: number; offline: number }> {
    return this.http.post<any>(`${this.base}/printers/poll-all`, {});
  }

  // Discovery

  runDiscovery(): Observable<DiscoveryResult> {
    return this.http.post<DiscoveryResult>(`${this.base}/discovery/run`, {});
  }
}
