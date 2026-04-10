import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PrinterService } from '../../core/services/printer.service';
import { Printer, PrinterStats, locationName } from '../../core/models/printer.model';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { TonerBarComponent } from '../../shared/components/toner-bar/toner-bar.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, StatusBadgeComponent, TonerBarComponent],
  template: `
    <div class="p-6 space-y-6">

      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p class="text-sm text-gray-500 mt-0.5">Printer toner monitoring</p>
        </div>
        <button class="btn-primary" (click)="pollAll()" [disabled]="polling()">
          <svg *ngIf="!polling()" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          <svg *ngIf="polling()" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
          </svg>
          {{ polling() ? 'Polling...' : 'Poll All' }}
        </button>
      </div>

      <!-- Stats -->
      <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4" *ngIf="stats()">
        <div class="card p-4 text-center">
          <p class="text-3xl font-bold text-gray-900">{{ stats()!.total }}</p>
          <p class="text-sm text-gray-500 mt-1">Total</p>
        </div>
        <div class="card p-4 text-center">
          <p class="text-3xl font-bold text-green-600">{{ stats()!.ok }}</p>
          <p class="text-sm text-gray-500 mt-1">OK</p>
        </div>
        <div class="card p-4 text-center">
          <p class="text-3xl font-bold text-yellow-500">{{ stats()!.warning }}</p>
          <p class="text-sm text-gray-500 mt-1">Warning</p>
        </div>
        <div class="card p-4 text-center">
          <p class="text-3xl font-bold text-red-600">{{ stats()!.error }}</p>
          <p class="text-sm text-gray-500 mt-1">Error</p>
        </div>
        <div class="card p-4 text-center">
          <p class="text-3xl font-bold text-gray-400">{{ stats()!.offline }}</p>
          <p class="text-sm text-gray-500 mt-1">Offline</p>
        </div>
      </div>

      <!-- Filter bar -->
      <div class="flex flex-wrap gap-3 items-center">
        <span class="text-sm font-medium text-gray-600">Filter:</span>
        @for (f of filters; track f.value) {
          <button
            class="px-3 py-1 rounded-full text-xs font-medium border transition-colors"
            [class]="activeFilter() === f.value
              ? 'bg-blue-600 text-white border-blue-600'
              : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'"
            (click)="activeFilter.set(f.value)">
            {{ f.label }}
          </button>
        }
        <input
          type="text"
          placeholder="Search printers..."
          class="ml-auto input w-1/5 min-w-40"
          (input)="search.set($any($event.target).value)" />
      </div>

      <!-- Printer grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        @for (printer of filteredPrinters(); track printer.ip_address) {
          <a [routerLink]="['/printers', printer.ip_address]"
            class="card p-4 hover:shadow-md transition-shadow cursor-pointer block">

            <!-- Card header -->
            <div class="flex items-start justify-between mb-3">
              <div class="min-w-0 flex-1">
                <p class="font-semibold text-gray-900 truncate">{{ printer.name }}</p>
                <p class="text-xs text-gray-400 mt-0.5">{{ printer.ip_address }}</p>
              </div>
              <app-status-badge [status]="printer.status" class="ml-2 flex-shrink-0" />
            </div>

            <!-- Toner bars (top 2 cartridges only in card view) -->
            <div class="space-y-1.5">
              @for (entry of tonerEntries(printer) | slice:0:2; track entry[0]) {
                <app-toner-bar [name]="entry[0]" [display]="entry[1]" />
              }
              @if (tonerEntries(printer).length > 2) {
                <p class="text-xs text-gray-400 text-right">
                  +{{ tonerEntries(printer).length - 2 }} more
                </p>
              }
            </div>

            <!-- Footer -->
            <div class="mt-3 pt-3 border-t border-gray-100 flex items-center justify-between">
              <span class="text-xs text-gray-400">{{ locationName(printer.location) }}</span>
              <span class="text-xs text-gray-400">
                {{ printer.last_polled_at ? timeAgo(printer.last_polled_at) : 'Never polled' }}
              </span>
            </div>

          </a>
        }

        @empty {
          <div class="col-span-full text-center py-16 text-gray-400">
            <svg class="w-12 h-12 mx-auto mb-3 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                d="M17 17H17.01M17 3H5a2 2 0 00-2 2v11a2 2 0 002 2h14a2 2 0 002-2V5a2 2 0 00-2-2zM11 7l2 2-2 2" />
            </svg>
            <p class="text-sm">No printers found</p>
          </div>
        }
      </div>

    </div>
  `,
})
export class DashboardComponent implements OnInit {
  private readonly svc = inject(PrinterService);

  printers = signal<Printer[]>([]);
  stats = signal<PrinterStats | null>(null);
  polling = signal(false);
  activeFilter = signal<string>('all');
  search = signal('');
  locationName = locationName;

  filters = [
    { label: 'All',     value: 'all' },
    { label: 'OK',      value: 'Ok' },
    { label: 'Warning', value: 'Warning' },
    { label: 'Error',   value: 'Error' },
    { label: 'Offline', value: 'Offline' },
  ];

  filteredPrinters() {
    return this.printers().filter(p => {
      const matchesFilter = this.activeFilter() === 'all' || p.status === this.activeFilter();
      const q = this.search().toLowerCase();
      const matchesSearch = !q ||
        p.name.toLowerCase().includes(q) ||
        p.ip_address.includes(q) ||
        p.location.toLowerCase().includes(q);
      return matchesFilter && matchesSearch;
    });
  }

  tonerEntries(printer: Printer): [string, string][] {
    return Object.entries(printer.toner_cartridges);
  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.svc.getAll().subscribe(p => this.printers.set(p));
    this.svc.getStats().subscribe(s => this.stats.set(s));
  }

  pollAll() {
    this.polling.set(true);
    this.svc.pollAll().subscribe({
      next: () => { this.polling.set(false); this.load(); },
      error: () => this.polling.set(false),
    });
  }

  timeAgo(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1)  return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24)  return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }
}
