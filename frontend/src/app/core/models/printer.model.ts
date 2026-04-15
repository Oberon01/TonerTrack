export type PrinterStatus = 'Unknown' | 'Ok' | 'Warning' | 'Error' | 'Offline';

export type PrinterBrand = 'HP' | 'Canon' | 'Printronix' | 'SATO' | 'Zebra' |'Other';

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

export function detectBrand(model:string | null | undefined): PrinterBrand {
  if (!model) return 'Other';
  const m = model.toLowerCase();
  if (m.includes('hp') || m.includes('hewlett')) return 'HP';
  if (m.includes('canon')) return 'Canon';
  if (m.includes('sato')) return 'SATO';
  if (m.includes('zebra')) return 'Zebra';
  
  // Printronix models typically start with T (e.g. T4000, T6000, T8000)
  // or contain "printronix" in the string
  if (m.includes('printronix') || /\bT\d{4}/i.test(model)) return 'Printronix';

  return 'Other';
}

export const BRANDS: PrinterBrand[] = ['HP', 'Canon', 'Printronix', 'SATO', 'Zebra', 'Other'];

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
