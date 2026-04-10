import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrinterStatus } from '../../../core/models/printer.model';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [class]="badgeClass">
      <span class="inline-block w-1.5 h-1.5 rounded-full mr-1.5" [class]="dotClass"></span>
      {{ status }}
    </span>
  `,
})
export class StatusBadgeComponent {
  @Input() status: PrinterStatus = 'Unknown';

  get badgeClass(): string {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    const map: Record<PrinterStatus, string> = {
      Ok:      `${base} bg-green-100 text-green-800`,
      Warning: `${base} bg-yellow-100 text-yellow-800`,
      Error:   `${base} bg-red-100 text-red-800`,
      Offline: `${base} bg-gray-100 text-gray-600`,
      Unknown: `${base} bg-gray-100 text-gray-500`,
    };
    return map[this.status] ?? map['Unknown'];
  }

  get dotClass(): string {
    const map: Record<PrinterStatus, string> = {
      Ok:      'bg-green-500',
      Warning: 'bg-yellow-500',
      Error:   'bg-red-500',
      Offline: 'bg-gray-400',
      Unknown: 'bg-gray-400',
    };
    return map[this.status] ?? 'bg-gray-400';
  }
}
