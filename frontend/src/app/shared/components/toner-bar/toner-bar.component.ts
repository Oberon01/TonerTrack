import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-toner-bar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex items-center gap-3">
      <span class="text-xs text-gray-500 w-32 truncate">{{ name }}</span>
      <div class="flex-1 bg-gray-100 rounded-full h-2.5 overflow-hidden">
        <div
          class="h-2.5 rounded-full transition-all duration-300"
          [class]="barColor"
          [style.width]="barWidth">
        </div>
      </div>
      <span class="text-xs font-medium w-10 text-right" [class]="textColor">
        {{ display }}
      </span>
    </div>
  `,
})
export class TonerBarComponent {
  @Input() name = '';
  @Input() display = '';

  get percentage(): number | null {
    if (!this.display || this.display === 'Unknown' || this.display === 'OK') return null;
    return parseInt(this.display.replace('%', ''), 10);
  }

  get barWidth(): string {
    const pct = this.percentage;
    if (pct === null) return '100%';
    return `${pct}%`;
  }

  get barColor(): string {
    const pct = this.percentage;
    if (pct === null) return 'bg-gray-300';
    if (pct < 10) return 'bg-red-500';
    if (pct < 20) return 'bg-yellow-500';
    return 'bg-green-500';
  }

  get textColor(): string {
    const pct = this.percentage;
    if (pct === null) return 'text-gray-400';
    if (pct < 10) return 'text-red-600';
    if (pct < 20) return 'text-yellow-600';
    return 'text-gray-700';
  }
}
