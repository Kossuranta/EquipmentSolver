import { Component, Inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import {
  SlotDto,
  StatTypeDto,
  SlotMappingEntry,
  StatMappingEntry,
  BulkEquipmentImportRequest,
} from '../../models/profile.models';
import { CsvEquipmentItem, findBestMatch } from '../../utils/csv.utils';

export interface ImportMappingDialogData {
  items: CsvEquipmentItem[];
  csvSlotNames: string[];
  csvStatNames: string[];
  profileSlots: SlotDto[];
  profileStatTypes: StatTypeDto[];
}

export interface ImportMappingDialogResult {
  request: BulkEquipmentImportRequest;
}

interface MappingRow {
  csvName: string;
  action: 'generate' | 'map' | 'ignore';
  mapToId: number | null;
}

@Component({
  selector: 'app-import-mapping-dialog',
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
  ],
  templateUrl: './import-mapping-dialog.component.html',
  styleUrl: './import-mapping-dialog.component.scss',
})
export class ImportMappingDialogComponent {
  slotMappings = signal<MappingRow[]>([]);
  statMappings = signal<MappingRow[]>([]);
  itemCount: number;

  constructor(
    private readonly dialogRef: MatDialogRef<ImportMappingDialogComponent>,
    @Inject(MAT_DIALOG_DATA) readonly data: ImportMappingDialogData,
  ) {
    this.itemCount = data.items.length;

    // Initialize slot mappings with fuzzy matching
    this.slotMappings.set(
      data.csvSlotNames.map(csvName => {
        const match = findBestMatch(csvName, data.profileSlots, s => s.name);
        return {
          csvName,
          action: match ? 'map' : 'generate',
          mapToId: match?.id ?? null,
        } satisfies MappingRow;
      }),
    );

    // Initialize stat mappings with fuzzy matching
    this.statMappings.set(
      data.csvStatNames.map(csvName => {
        const match = findBestMatch(csvName, data.profileStatTypes, st => st.displayName);
        return {
          csvName,
          action: match ? 'map' : 'generate',
          mapToId: match?.id ?? null,
        } satisfies MappingRow;
      }),
    );
  }

  updateSlotAction(index: number, action: 'generate' | 'map' | 'ignore'): void {
    const mappings = [...this.slotMappings()];
    mappings[index] = { ...mappings[index], action, mapToId: action === 'map' ? mappings[index].mapToId : null };
    this.slotMappings.set(mappings);
  }

  updateSlotTarget(index: number, slotId: number): void {
    const mappings = [...this.slotMappings()];
    mappings[index] = { ...mappings[index], mapToId: slotId };
    this.slotMappings.set(mappings);
  }

  updateStatAction(index: number, action: 'generate' | 'map' | 'ignore'): void {
    const mappings = [...this.statMappings()];
    mappings[index] = { ...mappings[index], action, mapToId: action === 'map' ? mappings[index].mapToId : null };
    this.statMappings.set(mappings);
  }

  updateStatTarget(index: number, statTypeId: number): void {
    const mappings = [...this.statMappings()];
    mappings[index] = { ...mappings[index], mapToId: statTypeId };
    this.statMappings.set(mappings);
  }

  isValid(): boolean {
    // All "map" rows must have a target selected
    for (const m of this.slotMappings()) {
      if (m.action === 'map' && m.mapToId === null) return false;
    }
    for (const m of this.statMappings()) {
      if (m.action === 'map' && m.mapToId === null) return false;
    }
    return true;
  }

  submit(): void {
    if (!this.isValid()) return;

    const slotMappings: SlotMappingEntry[] = this.slotMappings().map(m => ({
      csvSlotName: m.csvName,
      action: m.action,
      mapToSlotId: m.action === 'map' ? m.mapToId ?? undefined : undefined,
    }));

    const statMappings: StatMappingEntry[] = this.statMappings().map(m => ({
      csvStatName: m.csvName,
      action: m.action,
      mapToStatTypeId: m.action === 'map' ? m.mapToId ?? undefined : undefined,
    }));

    const request: BulkEquipmentImportRequest = {
      items: this.data.items.map(item => ({
        name: item.name,
        slotNames: item.slotNames,
        stats: item.stats.map(s => ({ statName: s.statName, value: s.value })),
      })),
      slotMappings,
      statMappings,
    };

    this.dialogRef.close({ request } satisfies ImportMappingDialogResult);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
