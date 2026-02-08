import { Component, Inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import {
  EquipmentDto,
  EquipmentStatInput,
  SlotDto,
  StatTypeDto,
} from '../../models/profile.models';

export interface EquipmentDialogData {
  mode: 'create' | 'edit';
  slots: SlotDto[];
  statTypes: StatTypeDto[];
  equipment?: EquipmentDto;
}

export interface EquipmentDialogResult {
  name: string;
  compatibleSlotIds: number[];
  stats: EquipmentStatInput[];
}

@Component({
  selector: 'app-equipment-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatSelectModule,
  ],
  templateUrl: './equipment-dialog.component.html',
  styleUrl: './equipment-dialog.component.scss',
})
export class EquipmentDialogComponent {
  nameControl = new FormControl('', [Validators.required, Validators.maxLength(200)]);
  selectedSlotIds = signal<Set<number>>(new Set());
  statEntries = signal<{ statTypeId: number; value: number }[]>([]);
  availableStatTypes = signal<StatTypeDto[]>([]);

  constructor(
    private readonly dialogRef: MatDialogRef<EquipmentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) readonly data: EquipmentDialogData,
  ) {
    if (data.mode === 'edit' && data.equipment) {
      this.nameControl.setValue(data.equipment.name);
      this.selectedSlotIds.set(new Set(data.equipment.compatibleSlotIds));
      this.statEntries.set(data.equipment.stats.map(s => ({ ...s })));
    }
    this.updateAvailableStats();
  }

  toggleSlot(slotId: number): void {
    const current = new Set(this.selectedSlotIds());
    if (current.has(slotId)) {
      current.delete(slotId);
    } else {
      current.add(slotId);
    }
    this.selectedSlotIds.set(current);
  }

  isSlotSelected(slotId: number): boolean {
    return this.selectedSlotIds().has(slotId);
  }

  addStat(): void {
    const available = this.availableStatTypes();
    if (available.length === 0) return;

    const entries = [...this.statEntries(), { statTypeId: available[0].id, value: 0 }];
    this.statEntries.set(entries);
    this.updateAvailableStats();
  }

  removeStat(index: number): void {
    const entries = this.statEntries().filter((_, i) => i !== index);
    this.statEntries.set(entries);
    this.updateAvailableStats();
  }

  updateStatType(index: number, statTypeId: number): void {
    const entries = [...this.statEntries()];
    entries[index] = { ...entries[index], statTypeId };
    this.statEntries.set(entries);
    this.updateAvailableStats();
  }

  updateStatValue(index: number, value: number): void {
    const entries = [...this.statEntries()];
    entries[index] = { ...entries[index], value };
    this.statEntries.set(entries);
  }

  private updateAvailableStats(): void {
    const usedIds = new Set(this.statEntries().map(e => e.statTypeId));
    this.availableStatTypes.set(this.data.statTypes.filter(st => !usedIds.has(st.id)));
  }

  getStatTypeName(statTypeId: number): string {
    return this.data.statTypes.find(st => st.id === statTypeId)?.displayName ?? 'Unknown';
  }

  /// Get stat types available for a specific entry (current + unused)
  getStatTypeOptions(currentStatTypeId: number): StatTypeDto[] {
    const usedIds = new Set(this.statEntries().map(e => e.statTypeId));
    return this.data.statTypes.filter(st => st.id === currentStatTypeId || !usedIds.has(st.id));
  }

  /** Convert comma to dot for decimal input (European locale support). */
  onDecimalKeydown(event: KeyboardEvent): void {
    if (event.key === ',') {
      event.preventDefault();
      document.execCommand('insertText', false, '.');
    }
  }

  isValid(): boolean {
    return this.nameControl.valid && this.selectedSlotIds().size > 0;
  }

  save(): void {
    if (!this.isValid()) return;

    const result: EquipmentDialogResult = {
      name: this.nameControl.value!.trim(),
      compatibleSlotIds: [...this.selectedSlotIds()],
      stats: this.statEntries().filter(s => s.value !== 0),
    };

    this.dialogRef.close(result);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
