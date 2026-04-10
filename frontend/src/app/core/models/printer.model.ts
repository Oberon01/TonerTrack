export type PrinterStatus = 'Unknown' | 'Ok' | 'Warning' | 'Error' | 'Offline';

export interface Printer {
  ip_address: string;
  name: string;
  community: string;
  location: string;
  model: string;
  serial_number: string;
  status: PrinterStatus;
  last_polled_at: string | null;
  total_pages_printed: string | null;
  offline_attempts: number;
  has_open_ticket: boolean;
  toner_cartridges: Record<string, string>;
  drum_units: Record<string, string>;
  other_supplies: Record<string, string>;
  monthly_pages: Record<string, string>;
}

export interface PrinterStats {
  total: number;
  ok: number;
  warning: number;
  error: number;
  offline: number;
  unknown: number;
}

export interface PrinterListResponse {
  printers: Printer[];
}

export interface UsageDto {
  ip_address: string;
  name: string;
  last_6_months: MonthlyUsage[];
  average_last_6: number;
  last_month: number | null;
  month_change_percent: number | null;
  full_history: Record<string, number>;
}

export interface MonthlyUsage {
  month: string;
  pages: number;
}

export interface DiscoveryResult {
  added: number;
  updated: number;
  skipped: number;
  total_found: number;
  sources_run: string[];
  details: DiscoveryDetail[];
}

export interface DiscoveryDetail {
  ip_address: string;
  name: string;
  action: 'added' | 'updated' | 'skipped';
  source: string;
}

export interface AddPrinterRequest {
  name: string;
  ip_address: string;
  community?: string;
  location?: string;
}

export interface UpdatePrinterRequest {
  name?: string;
  community?: string;
  location?: string;
}
