import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { PrinterService } from '../../../core/services/printer.service';

@Component({
  selector: 'app-printer-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="p-6 max-w-xl">

      <!-- Header -->
      <div class="flex items-center gap-3 mb-6">
        <a routerLink="/printers" class="text-gray-400 hover:text-gray-600">
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 class="text-2xl font-bold text-gray-900">
          {{ isEdit() ? 'Edit Printer' : 'Add Printer' }}
        </h1>
      </div>

      <div class="card p-6">
        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">

          <div>
            <label class="label">Printer Name *</label>
            <input formControlName="name" type="text" class="input"
              placeholder="e.g. Finance Floor Printer" />
            <p *ngIf="form.get('name')?.invalid && form.get('name')?.touched"
              class="text-red-500 text-xs mt-1">Name is required</p>
          </div>

          <div>
            <label class="label">IP Address *</label>
            <input formControlName="ip_address" type="text" class="input"
              placeholder="e.g. 10.10.5.17" [readonly]="isEdit()" />
            <p *ngIf="form.get('ip_address')?.invalid && form.get('ip_address')?.touched"
              class="text-red-500 text-xs mt-1">Valid IP address is required</p>
          </div>

          <div>
            <label class="label">SNMP Community</label>
            <input formControlName="community" type="text" class="input"
              placeholder="public" />
          </div>

          <div>
            <label class="label">Location ID</label>
            <input formControlName="location" type="text" class="input"
              placeholder="e.g. 1" />
            <p class="text-xs text-gray-400 mt-1">NinjaRMM location ID for ticket routing</p>
          </div>

          <!-- Error message -->
          <div *ngIf="error()" class="rounded-lg bg-red-50 border border-red-200 p-3 text-sm text-red-700">
            {{ error() }}
          </div>

          <div class="flex gap-3 pt-2">
            <button type="submit" class="btn-primary" [disabled]="form.invalid || saving()">
              <svg *ngIf="saving()" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
              </svg>
              {{ saving() ? 'Saving...' : (isEdit() ? 'Save Changes' : 'Add Printer') }}
            </button>
            <a routerLink="/printers" class="btn-secondary">Cancel</a>
          </div>

        </form>
      </div>

    </div>
  `,
})
export class PrinterFormComponent implements OnInit {
  private readonly svc    = inject(PrinterService);
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  isEdit = signal(false);
  saving = signal(false);
  error  = signal('');

  form = this.fb.group({
    name:       ['', Validators.required],
    ip_address: ['', [Validators.required,
      Validators.pattern(/^(\d{1,3}\.){3}\d{1,3}$/)]],
    community:  ['public'],
    location:   [''],
  });

  get ip(): string {
    return this.route.snapshot.paramMap.get('ip') ?? '';
  }

  ngOnInit() {
    if (this.ip) {
      this.isEdit.set(true);
      this.svc.getByIp(this.ip).subscribe(p => {
        this.form.patchValue({
          name:       p.name,
          ip_address: p.ip_address,
          community:  p.community,
          location:   p.location,
        });
        this.form.get('ip_address')?.disable();
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set('');

    const val = this.form.getRawValue();

    const req$ = this.isEdit()
      ? this.svc.update(this.ip, { name: val.name!, community: val.community!, location: val.location! })
      : this.svc.add({ name: val.name!, ip_address: val.ip_address!, community: val.community!, location: val.location! });

    req$.subscribe({
      next: p => this.router.navigate(['/printers', p.ip_address]),
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.title ?? 'An error occurred. Please try again.');
      },
    });
  }
}
