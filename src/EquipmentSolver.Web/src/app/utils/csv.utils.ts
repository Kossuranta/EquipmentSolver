import { EquipmentDto, SlotDto, StatTypeDto } from '../models/profile.models';

// --- CSV Parsing (RFC 4180 compliant) ---

export interface ParsedCsv {
  typeRow: string[];
  nameRow: string[];
  dataRows: string[][];
}

/**
 * Parse a CSV string with two header rows (type + name) and data rows.
 * Handles quoted fields per RFC 4180.
 */
export function parseCsv(text: string): ParsedCsv {
  const rows = parseCsvRows(text);
  if (rows.length < 2) {
    throw new Error('CSV must have at least two header rows (type row and name row).');
  }

  return {
    typeRow: rows[0],
    nameRow: rows[1],
    dataRows: rows.slice(2),
  };
}

/**
 * Parse CSV text into array of rows, each row an array of fields.
 * Handles quoted fields, escaped quotes (""), and newlines within quotes.
 */
function parseCsvRows(text: string): string[][] {
  const rows: string[][] = [];
  let current: string[] = [];
  let field = '';
  let inQuotes = false;
  let i = 0;

  while (i < text.length) {
    const ch = text[i];

    if (inQuotes) {
      if (ch === '"') {
        if (i + 1 < text.length && text[i + 1] === '"') {
          field += '"';
          i += 2;
        } else {
          inQuotes = false;
          i++;
        }
      } else {
        field += ch;
        i++;
      }
    } else {
      if (ch === '"') {
        inQuotes = true;
        i++;
      } else if (ch === ',') {
        current.push(field);
        field = '';
        i++;
      } else if (ch === '\r') {
        // Skip \r, handle \n next
        i++;
      } else if (ch === '\n') {
        current.push(field);
        field = '';
        rows.push(current);
        current = [];
        i++;
      } else {
        field += ch;
        i++;
      }
    }
  }

  // Push last field/row if there's remaining content
  if (field || current.length > 0) {
    current.push(field);
    rows.push(current);
  }

  return rows;
}

// --- CSV Generation ---

/**
 * Quote a CSV field if it contains commas, quotes, or newlines (RFC 4180).
 */
function csvQuote(value: string): string {
  if (value.includes(',') || value.includes('"') || value.includes('\n') || value.includes('\r')) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}

/**
 * Build a CSV string from equipment data for export.
 */
export function buildEquipmentCsv(
  equipment: EquipmentDto[],
  slots: SlotDto[],
  statTypes: StatTypeDto[],
): string {
  const lines: string[] = [];

  // Type row
  const typeRow = ['Name', ...slots.map(() => 'Slot'), ...statTypes.map(() => 'Stat')];
  lines.push(typeRow.map(csvQuote).join(','));

  // Name row
  const nameRow = ['Name', ...slots.map(s => s.name), ...statTypes.map(st => st.displayName)];
  lines.push(nameRow.map(csvQuote).join(','));

  // Data rows
  for (const item of equipment) {
    const slotValues = slots.map(s => (item.compatibleSlotIds.includes(s.id) ? 'X' : ''));
    const statValues = statTypes.map(st => {
      const stat = item.stats.find(es => es.statTypeId === st.id);
      return stat ? String(stat.value) : '0';
    });
    const row = [csvQuote(item.name), ...slotValues, ...statValues.map(csvQuote)];
    lines.push(row.join(','));
  }

  return lines.join('\n') + '\n';
}

// --- CSV Import Data Extraction ---

export interface CsvColumnInfo {
  index: number;
  type: 'Slot' | 'Stat';
  name: string;
}

export interface CsvEquipmentItem {
  name: string;
  slotNames: string[];
  stats: { statName: string; value: number }[];
}

/**
 * Extract column metadata and equipment items from parsed CSV.
 */
export function extractCsvData(parsed: ParsedCsv): {
  columns: CsvColumnInfo[];
  items: CsvEquipmentItem[];
} {
  const columns: CsvColumnInfo[] = [];

  // Parse column types from type/name rows (skip index 0 = Name column)
  for (let i = 1; i < parsed.typeRow.length; i++) {
    const type = parsed.typeRow[i]?.trim();
    const name = parsed.nameRow[i]?.trim();
    if (!name) continue;

    if (type?.toLowerCase() === 'slot') {
      columns.push({ index: i, type: 'Slot', name });
    } else if (type?.toLowerCase() === 'stat') {
      columns.push({ index: i, type: 'Stat', name });
    }
  }

  const slotColumns = columns.filter(c => c.type === 'Slot');
  const statColumns = columns.filter(c => c.type === 'Stat');

  // Parse data rows
  const items: CsvEquipmentItem[] = [];
  for (const row of parsed.dataRows) {
    const name = row[0]?.trim();
    if (!name) continue;

    const slotNames: string[] = [];
    for (const col of slotColumns) {
      const val = row[col.index]?.trim().toUpperCase();
      if (val === 'X') {
        slotNames.push(col.name);
      }
    }

    const stats: { statName: string; value: number }[] = [];
    for (const col of statColumns) {
      let raw = row[col.index]?.trim() ?? '';
      // Normalize comma decimal separator to dot
      raw = raw.replace(',', '.');
      const value = parseFloat(raw);
      if (!isNaN(value) && value !== 0) {
        stats.push({ statName: col.name, value });
      }
    }

    items.push({ name, slotNames, stats });
  }

  return { columns, items };
}

// --- Fuzzy Matching ---

/**
 * Normalize a name for fuzzy matching: lowercase, strip spaces/underscores/hyphens.
 */
export function normalizeName(name: string): string {
  return name.toLowerCase().replace(/[\s_-]/g, '');
}

/**
 * Compute Levenshtein distance between two strings.
 */
export function levenshtein(a: string, b: string): number {
  const m = a.length;
  const n = b.length;
  const dp: number[][] = Array.from({ length: m + 1 }, () => Array(n + 1).fill(0));

  for (let i = 0; i <= m; i++) dp[i][0] = i;
  for (let j = 0; j <= n; j++) dp[0][j] = j;

  for (let i = 1; i <= m; i++) {
    for (let j = 1; j <= n; j++) {
      dp[i][j] =
        a[i - 1] === b[j - 1]
          ? dp[i - 1][j - 1]
          : 1 + Math.min(dp[i - 1][j], dp[i][j - 1], dp[i - 1][j - 1]);
    }
  }

  return dp[m][n];
}

/**
 * Find the best matching existing item for a CSV name.
 * Returns the matched item or null if no good match.
 */
export function findBestMatch<T>(
  csvName: string,
  existing: T[],
  getName: (item: T) => string,
): T | null {
  const normalized = normalizeName(csvName);

  // Exact normalized match
  const exact = existing.find(item => normalizeName(getName(item)) === normalized);
  if (exact) return exact;

  // Short names (≤ 4 chars): only exact match
  if (normalized.length <= 4) return null;

  // Levenshtein distance ≤ 2 on normalized names
  return (
    existing.find(item => levenshtein(normalizeName(getName(item)), normalized) <= 2) ?? null
  );
}

// --- File Utils ---

/**
 * Trigger a browser file download.
 */
export function downloadFile(content: string | Blob, filename: string, mimeType: string): void {
  const blob = content instanceof Blob ? content : new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

/**
 * Read a file as text using FileReader API.
 */
export function readFileAsText(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(new Error('Failed to read file.'));
    reader.readAsText(file);
  });
}
