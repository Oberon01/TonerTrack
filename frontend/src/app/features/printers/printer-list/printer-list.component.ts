import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { PrinterService } from '../../../core/services/printer.service';
import { Printer, locationName, detectBrand, BRANDS, PrinterBrand } from '../../../core/models/printer.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-printer-list',
  standalone: true,
  imports: [CommonModule, RouterModule, StatusBadgeComponent],
  template: `
    <div class="p-6 space-y-4">

      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Printers</h1>
          <p class="text-sm text-gray-500 mt-0.5">{{ printers().length }} total</p>
        </div>
        <a routerLink="/printers/add" class="btn-primary">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Add Printer
        </a>
      </div>

      <!-- Search -->
      <div class="card p-4 flex flex-wrap gap-3 items-center">
        <input
          type="text"
          placeholder="Search by name, IP, or location..."
          class="input flex-1 min-w-48"
          (input)="search.set($any($event.target).value)" />
        <select class="input w-40" (change)="statusFilter.set($any($event.target).value)">
          <option value="">All statuses</option>
          <option value="Ok">OK</option>
          <option value="Warning">Warning</option>
          <option value="Error">Error</option>
          <option value="Offline">Offline</option>
          <option value="Unknown">Unknown</option>
        </select>
        <select class="input w-40" (change)="brandFilter.set($any($event.target).value)">
          <option value="">All brands</option>
          <option value="HP">HP</option>
          <option value="Canon">Canon</option>
          <option value="Printronix">Printronix</option>
          <option value="SATO">SATO</option>
          <option value="Other">Other</option>
        </select>
      </div>

      <!-- Table -->
      <div class="card overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 border-b border-gray-200">
            <tr>
              <th class="px-4 py-3 text-left font-medium text-gray-600 cursor-pointer" (click)="toggleSort('name')">Name
                <span class="ml-2 inline-flex items-center text-gray-400">
                  <svg *ngIf="sortBy() === 'name' && sortDir() === 'asc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <svg *ngIf="sortBy() === 'name' && sortDir() === 'desc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </span>
              </th>
              <th class="px-4 py-3 text-left font-medium text-gray-600 cursor-pointer" (click)="toggleSort('ip_address')">IP Address
                <span class="ml-2 inline-flex items-center text-gray-400">
                  <svg *ngIf="sortBy() === 'ip_address' && sortDir() === 'asc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <svg *ngIf="sortBy() === 'ip_address' && sortDir() === 'desc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </span>
              </th>
              <th class="px-4 py-3 text-left font-medium text-gray-600 cursor-pointer" (click)="toggleSort('status')">Status
                <span class="ml-2 inline-flex items-center text-gray-400">
                  <svg *ngIf="sortBy() === 'status' && sortDir() === 'asc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <svg *ngIf="sortBy() === 'status' && sortDir() === 'desc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </span>
              </th>
              <th class="px-4 py-3 text-left font-medium text-gray-600 cursor-pointer" (click)="toggleSort('location')">Location
                <span class="ml-2 inline-flex items-center text-gray-400">
                  <svg *ngIf="sortBy() === 'location' && sortDir() === 'asc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <svg *ngIf="sortBy() === 'location' && sortDir() === 'desc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </span>
              </th>
              <th class="px-4 py-3 text-left font-medium text-gray-600 cursor-pointer" (click)="toggleSort('model')">Model
                <span class="ml-2 inline-flex items-center text-gray-400">
                  <svg *ngIf="sortBy() === 'model' && sortDir() === 'asc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <svg *ngIf="sortBy() === 'model' && sortDir() === 'desc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </span>
              </th>
              <th class="px-4 py-3 text-left font-medium text-gray-600 cursor-pointer" (click)="toggleSort('last_polled_at')">Last Polled
                <span class="ml-2 inline-flex items-center text-gray-400">
                  <svg *ngIf="sortBy() === 'last_polled_at' && sortDir() === 'asc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <svg *ngIf="sortBy() === 'last_polled_at' && sortDir() === 'desc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </span>
              </th>
              <th class="px-4 py-3 text-left font-medium text-gray-600 cursor-pointer" (click)="toggleSort('has_open_ticket')">Ticket
                <span class="ml-2 inline-flex items-center text-gray-400">
                  <svg *ngIf="sortBy() === 'has_open_ticket' && sortDir() === 'asc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <svg *ngIf="sortBy() === 'has_open_ticket' && sortDir() === 'desc'" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </span>
              </th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            @for (printer of filtered(); track printer.ip_address) {
              <tr class="hover:bg-gray-50 transition-colors cursor-pointer" (click)="goTo(printer.ip_address)">
                <td class="px-4 py-3 font-medium text-gray-900">{{ printer.name }}</td>
                <td class="px-4 py-3 text-gray-500 font-mono text-xs">{{ printer.ip_address }}</td>
                <td class="px-4 py-3">
                  <app-status-badge [status]="printer.status" />
                </td>
                <td class="px-4 py-3 text-gray-500">{{ locationName(printer.location) }}</td>
                <td class="px-4 py-3 text-gray-500 truncate max-w-40">{{ printer.model }}</td>
                <td class="px-4 py-3 text-gray-400 text-xs">
                  {{ printer.last_polled_at ? (printer.last_polled_at | date:'short') : 'Never' }}
                </td>
                <td class="px-4 py-3">
                  @if (printer.has_open_ticket) {
                    <span class="inline-flex items-center gap-1 text-xs text-orange-600 font-medium">
                      <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
                      </svg>
                      Open
                    </span>
                  }
                </td>
                <td class="px-4 py-3 text-right">
                  
                </td>
              </tr>
            }
            @empty {
              <tr>
                <td colspan="8" class="px-4 py-12 text-center text-gray-400 text-sm">
                  No printers match your search
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

    </div>
  `,
})
export class PrinterListComponent implements OnInit {
  private readonly svc = inject(PrinterService);
  private readonly router = inject(Router);

  printers = signal<Printer[]>([]);
  search = signal('');
  statusFilter = signal('');
  locationName = locationName;
  sortBy = signal<'ip_address' | 'name' | 'status' | 'location' | 'model' | 'last_polled_at' | 'has_open_ticket'>('ip_address');
  sortDir = signal<'asc' | 'desc'>('asc');
  brandFilter = signal<string>('');
  detectBrand = detectBrand

  filtered() {
    const q = this.search().toLowerCase();
    const filtered = this.printers().filter(p => {
      const matchesSearch = !q ||
        p.name.toLowerCase().includes(q) ||
        p.ip_address.includes(q) ||
        p.location.toLowerCase().includes(q) ||
        p.model.toLowerCase().includes(q);
      const matchesStatus = !this.statusFilter() || p.status === this.statusFilter();
      const matchesBrand = !this.brandFilter() || detectBrand(p.model) === this.brandFilter();
      return matchesSearch && matchesStatus && matchesBrand;
    });

    const field = this.sortBy();
    const dir = this.sortDir() === 'asc' ? 1 : -1;

    return filtered.sort((a, b) => {
      if (field === 'ip_address') {
        return dir * (this.ipToNumber(a.ip_address) - this.ipToNumber(b.ip_address));
      }
      if (field === 'last_polled_at') {
        const ta = a.last_polled_at ? new Date(a.last_polled_at).getTime() : 0;
        const tb = b.last_polled_at ? new Date(b.last_polled_at).getTime() : 0;
        return dir * (ta - tb);
      }
      if (field === 'has_open_ticket') {
        return dir * ((a.has_open_ticket ? 1 : 0) - (b.has_open_ticket ? 1 : 0));
      }
      const va = ((a as any)[field] ?? '').toString().toLowerCase();
      const vb = ((b as any)[field] ?? '').toString().toLowerCase();
      if (va < vb) return -1 * dir;
      if (va > vb) return 1 * dir;
      return 0;
    });
  }

  private ipToNumber(ip: string) {
    return ip.split('.').reduce((acc, oct) => (acc << 8) + (Number(oct) || 0), 0) >>> 0;
  }

  goTo(ip: string) {
    this.router.navigate(['/printers', ip]);
  }

  toggleSort(field: 'ip_address' | 'name' | 'status' | 'location' | 'model' | 'last_polled_at' | 'has_open_ticket') {
    if (this.sortBy() === field) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(field as any);
      this.sortDir.set('asc');
    }
  }

  ngOnInit() {
    this.svc.getAll().subscribe(p => this.printers.set(p));
  }
}
