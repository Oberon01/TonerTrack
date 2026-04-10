import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrinterService } from '../../core/services/printer.service';
import { DiscoveryResult } from '../../core/models/printer.model';

@Component({
  selector: 'app-discovery',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="p-6 space-y-6">

      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Printer Discovery</h1>
          <p class="text-sm text-gray-500 mt-0.5">
            Scan your network and print servers to find and sync printers
          </p>
        </div>
        <button class="btn-primary" (click)="run()" [disabled]="running()">
          <svg *ngIf="!running()" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <svg *ngIf="running()" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
          </svg>
          {{ running() ? 'Scanning...' : 'Run Discovery' }}
        </button>
      </div>

      <!-- Running state -->
      <div *ngIf="running()" class="card p-8 text-center">
        <div class="animate-spin w-10 h-10 border-4 border-blue-600 border-t-transparent rounded-full mx-auto mb-4"></div>
        <p class="font-medium text-gray-700">Discovery in progress...</p>
        <p class="text-sm text-gray-400 mt-1">
          Scanning network ranges and querying print servers. This may take a minute.
        </p>
      </div>

      <!-- Results -->
      <ng-container *ngIf="result() && !running()">

        <!-- Summary cards -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <div class="card p-4 text-center">
            <p class="text-3xl font-bold text-gray-900">{{ result()!.total_found }}</p>
            <p class="text-sm text-gray-500 mt-1">Found</p>
          </div>
          <div class="card p-4 text-center">
            <p class="text-3xl font-bold text-green-600">{{ result()!.added }}</p>
            <p class="text-sm text-gray-500 mt-1">Added</p>
          </div>
          <div class="card p-4 text-center">
            <p class="text-3xl font-bold text-blue-600">{{ result()!.updated }}</p>
            <p class="text-sm text-gray-500 mt-1">Updated</p>
          </div>
          <div class="card p-4 text-center">
            <p class="text-3xl font-bold text-gray-400">{{ result()!.skipped }}</p>
            <p class="text-sm text-gray-500 mt-1">Skipped</p>
          </div>
        </div>

        <!-- Sources run -->
        <div class="flex gap-2 items-center text-sm text-gray-500">
          <span>Sources:</span>
          @for (source of result()!.sources_run; track source) {
            <span class="px-2 py-0.5 bg-gray-100 rounded text-gray-700 font-medium text-xs">
              {{ source }}
            </span>
          }
        </div>

        <!-- Detail table -->
        <div class="card overflow-hidden">
          <div class="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
            <h2 class="font-semibold text-gray-800">Discovery Details</h2>
            <div class="flex gap-2">
              @for (f of actionFilters; track f.value) {
                <button
                  class="px-2.5 py-1 rounded text-xs font-medium border transition-colors"
                  [class]="detailFilter() === f.value
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'"
                  (click)="detailFilter.set(f.value)">
                  {{ f.label }}
                </button>
              }
            </div>
          </div>
          <table class="w-full text-sm">
            <thead class="bg-gray-50 border-b border-gray-200">
              <tr>
                <th class="px-4 py-3 text-left font-medium text-gray-600">IP Address</th>
                <th class="px-4 py-3 text-left font-medium text-gray-600">Name</th>
                <th class="px-4 py-3 text-left font-medium text-gray-600">Source</th>
                <th class="px-4 py-3 text-left font-medium text-gray-600">Action</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              @for (detail of filteredDetails(); track detail.ip_address + detail.source) {
                <tr class="hover:bg-gray-50">
                  <td class="px-4 py-3 font-mono text-xs text-gray-500">{{ detail.ip_address }}</td>
                  <td class="px-4 py-3 font-medium text-gray-900">{{ detail.name }}</td>
                  <td class="px-4 py-3 text-gray-500 text-xs">{{ detail.source }}</td>
                  <td class="px-4 py-3">
                    <span [class]="actionBadge(detail.action)">{{ detail.action }}</span>
                  </td>
                </tr>
              }
              @empty {
                <tr>
                  <td colspan="4" class="px-4 py-8 text-center text-gray-400 text-sm">
                    No results for this filter
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      </ng-container>

      <div *ngIf="error()" class="card p-4 bg-red-50 border border-red-200 text-red-700">
        {{ error() }}
      </div>

      <!-- Empty state -->
      <div *ngIf="!result() && !running()" class="card p-12 text-center">
        <svg class="w-12 h-12 mx-auto mb-3 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
            d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <p class="text-gray-500 font-medium">No discovery results yet</p>
        <p class="text-sm text-gray-400 mt-1">Click "Run Discovery" to scan your network</p>
      </div>

    </div>
  `,
})
export class DiscoveryComponent {
  private readonly svc = inject(PrinterService);

  running      = signal(false);
  result       = signal<DiscoveryResult | null>(null);
  detailFilter = signal<string>('all');
  error = signal('');

  actionFilters = [
    { label: 'All',     value: 'all' },
    { label: 'Added',   value: 'added' },
    { label: 'Updated', value: 'updated' },
    { label: 'Skipped', value: 'skipped' },
  ];

  filteredDetails() {
    const r = this.result();
    if (!r) return [];
    if (this.detailFilter() === 'all') return r.details;
    return r.details.filter(d => d.action === this.detailFilter());
  }

  actionBadge(action: string): string {
    const base = 'px-2 py-0.5 rounded text-xs font-medium';
    const map: Record<string, string> = {
      added:   `${base} bg-green-100 text-green-700`,
      updated: `${base} bg-blue-100 text-blue-700`,
      skipped: `${base} bg-gray-100 text-gray-500`,
    };
    return map[action] ?? `${base} bg-gray-100 text-gray-500`;
  }

  run() {
    this.running.set(true);
    this.result.set(null);
    this.error.set('');
    this.svc.runDiscovery().subscribe({
      next: r => { this.result.set(r); this.running.set(false); },
      error: err => {
        this.running.set(false);
        this.error.set(
          err?.error?.title ??
          err?.error?.message ??
          err?.message ??
          `HTTP ${err?.status}: ${err?.statusText}`
        );
        console.error('Discovery error:', err);
      },
    });
  }
}
