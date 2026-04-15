import { Injectable, inject, signal } from "@angular/core";
import { PrinterService } from "./printer.service";

@Injectable({ providedIn: 'root' })
export class LocationService {
    private readonly svc = inject(PrinterService);

    names = signal<Record<string, string>>({});

    load(){
        this.svc.getLocations().subscribe(n => this.names.set(n));
    }

    getName(id: string | null | undefined): string {
        if (!id) return '-';
        return this.names()[id] ?? id;
    }
}