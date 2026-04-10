import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { NgChartsModule } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { PrinterService } from '../../../core/services/printer.service';
import { Printer, UsageDto, locationName } from '../../../core/models/printer.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { TonerBarComponent } from '../../../shared/components/toner-bar/toner-bar.component';

@Component({
  selector: 'app-printer-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, NgChartsModule, StatusBadgeComponent, TonerBarComponent],
  template: `
    <div class="p-6 space-y-6" *ngIf="printer(); else loading">
      <div class="flex items-center gap-3">
        <a routerLink="/printers" class="text-gray-400 hover:text-gray-600">
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <div class="flex-1 min-w-0">
          <h1 class="text-2xl font-bold text-gray-900 truncate">{{ printer()!.name }}</h1>
          <p class="text-sm text-gray-400 font-mono">{{ printer()!.ip_address }}</p>
        </div>
        <div class="flex gap-2">
          <button class="btn-secondary" (click)="poll()" [disabled]="polling()">
            <svg class="w-4 h-4" [class.animate-spin]="polling()" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            {{ polling() ? 'Polling...' : 'Poll Now' }}
          </button>
          <a [routerLink]="['/printers', printer()!.ip_address, 'edit']" class="btn-secondary">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
            Edit
          </a>
          <button class="btn-danger" (click)="confirmDelete()">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
            Delete
          </button>
        </div>
      </div>

      <!-- Info + Toner grid -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">

        <!-- Printer info -->
        <div class="card p-5 space-y-3">
          <h2 class="font-semibold text-gray-800">Printer Info</h2>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-gray-500">Status</dt>
              <dd><app-status-badge [status]="printer()!.status" /></dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">Model</dt>
              <dd class="text-gray-900 text-right">{{ printer()!.model }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">Serial</dt>
              <dd class="text-gray-900 font-mono text-xs">{{ printer()!.serial_number }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">Location</dt>
              <dd class="text-gray-900">{{ locationName(printer()!.location) }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">Community</dt>
              <dd class="text-gray-900 font-mono text-xs">{{ printer()!.community }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">Total Pages</dt>
              <dd class="text-gray-900">{{ printer()!.total_pages_printed ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">Last Polled</dt>
              <dd class="text-gray-900">
                {{ printer()!.last_polled_at ? (printer()!.last_polled_at | date:'medium') : 'Never' }}
              </dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">Open Ticket</dt>
              <dd>
                <span *ngIf="printer()!.has_open_ticket" class="text-orange-600 font-medium text-xs">Yes</span>
                <span *ngIf="!printer()!.has_open_ticket" class="text-gray-400 text-xs">No</span>
              </dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-gray-500">
                <a href="http://{{ printer()!.ip_address }}" target="_blank" class="text-blue-600 hover:underline">
                  Access Printer Via Web Interface
                </a>
              </dt>
            </div>
          </dl>
        </div>

        <!-- Supplies -->
        <div class="card p-5 space-y-4">
          <h2 class="font-semibold text-gray-800">Supplies</h2>

          <div *ngIf="tonerEntries().length > 0">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wide mb-2">Toner</p>
            <div class="space-y-2">
              @for (entry of tonerEntries(); track entry[0]) {
                <app-toner-bar [name]="entry[0]" [display]="entry[1]" />
              }
            </div>
          </div>

          <div *ngIf="drumEntries().length > 0">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wide mb-2">Drums</p>
            <div class="space-y-2">
              @for (entry of drumEntries(); track entry[0]) {
                <app-toner-bar [name]="entry[0]" [display]="entry[1]" />
              }
            </div>
          </div>

          <div *ngIf="otherEntries().length > 0">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wide mb-2">Other</p>
            <div class="space-y-2">
              @for (entry of otherEntries(); track entry[0]) {
                <app-toner-bar [name]="entry[0]" [display]="entry[1]" />
              }
            </div>
          </div>

        </div>
      </div>

      <!-- Page history chart -->
      <div class="card p-5" *ngIf="usage()">
        <h2 class="font-semibold text-gray-800 mb-4">Page History (Last 6 Months)</h2>
        <div class="flex gap-6 text-sm mb-4">
          <div>
            <span class="text-gray-500">Monthly Average</span>
            <p class="font-semibold text-gray-900">{{ usage()!.average_last_6 | number }}</p>
          </div>
          <div *ngIf="usage()!.last_month !== null">
            <span class="text-gray-500">Last Month</span>
            <p class="font-semibold text-gray-900">
              {{ usage()!.last_month | number }}
              <span *ngIf="usage()!.month_change_percent !== null"
                [class]="usage()!.month_change_percent! >= 0 ? 'text-green-600' : 'text-red-600'"
                class="text-xs font-normal ml-1">
                {{ usage()!.month_change_percent! >= 0 ? '+' : '' }}{{ usage()!.month_change_percent }}%
              </span>
            </p>
          </div>
        </div>
        <canvas baseChart
          [data]="chartData()"
          [options]="chartOptions"
          type="bar"
          height="80">
        </canvas>
      </div>

    </div>

    <ng-template #loading>
      <div class="p-6 flex items-center justify-center h-64">
        <div class="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full"></div>
      </div>
    </ng-template>
  `,
})
export class PrinterDetailComponent implements OnInit {
  private readonly svc = inject(PrinterService);
  private readonly route = inject(ActivatedRoute);

  printer = signal<Printer | null>(null);
  usage = signal<UsageDto | null>(null);
  polling = signal(false);
  locationName = locationName;

  chartOptions: ChartOptions = {
    responsive: true,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true } },
  };

  chartData(): ChartData<'bar'> {
    const u = this.usage();
    if (!u) return { labels: [], datasets: [] };
    const sorted = [...u.last_6_months].sort((a, b) => a.month.localeCompare(b.month));
    return {
      labels: sorted.map(m => m.month),
      datasets: [{
        data: sorted.map(m => m.pages),
        backgroundColor: 'rgba(59, 130, 246, 0.6)',
        borderColor: 'rgb(59, 130, 246)',
        borderWidth: 1,
        borderRadius: 4,
      }],
    };
  }

  get ip(): string {
    return this.route.snapshot.paramMap.get('ip') ?? '';
  }

  tonerEntries = () => Object.entries(this.printer()?.toner_cartridges ?? {});
  drumEntries  = () => Object.entries(this.printer()?.drum_units ?? {});
  otherEntries = () => Object.entries(this.printer()?.other_supplies ?? {});

  ngOnInit() {
    this.svc.getByIp(this.ip).subscribe(p => this.printer.set(p));
    this.svc.getUsage(this.ip).subscribe(u => this.usage.set(u));
  }

  poll() {
    this.polling.set(true);
    this.svc.poll(this.ip).subscribe({
      next: p => { this.printer.set(p); this.polling.set(false); },
      error: () => this.polling.set(false),
    });
  }

  confirmDelete() {
    if (confirm(`Delete ${this.printer()?.name}? This cannot be undone.`)) {
      this.svc.delete(this.ip).subscribe(() => history.back());
    }
  }
}
