import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { ProfileService } from '../../services/profile.service';
import {
  EquipmentDto,
  SlotDto,
  StatTypeDto,
} from '../../models/profile.models';
import { EquipmentDialogComponent, EquipmentDialogData } from '../equipment-dialog/equipment-dialog.component';
import { ConfirmDialogComponent, ConfirmDialogData } from '../confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-profile-equipment-tab',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatExpansionModule,
    MatChipsModule,
    MatTooltipModule,
  ],
  templateUrl: './profile-equipment-tab.component.html',
  styleUrl: './profile-equipment-tab.component.scss',
})
export class ProfileEquipmentTabComponent {
  @Input({ required: true }) profileId!: number;
  @Input({ required: true }) equipment!: EquipmentDto[];
  @Input({ required: true }) slots!: SlotDto[];
  @Input({ required: true }) statTypes!: StatTypeDto[];
  @Input({ required: true }) isOwner!: boolean;
  @Output() equipmentChanged = new EventEmitter<EquipmentDto[]>();

  error = signal<string | null>(null);

  openAddDialog(): void {
    const dialogRef = this.dialog.open(EquipmentDialogComponent, {
      width: '600px',
      data: {
        mode: 'create',
        slots: this.slots,
        statTypes: this.statTypes,
      } satisfies EquipmentDialogData,
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.error.set(null);
      this.profileService
        .createEquipment(this.profileId, {
          name: result.name,
          compatibleSlotIds: result.compatibleSlotIds,
          stats: result.stats,
        })
        .subscribe({
          next: eq => this.equipmentChanged.emit([...this.equipment, eq]),
          error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to add equipment.'),
        });
    });
  }

  openEditDialog(item: EquipmentDto): void {
    const dialogRef = this.dialog.open(EquipmentDialogComponent, {
      width: '600px',
      data: {
        mode: 'edit',
        slots: this.slots,
        statTypes: this.statTypes,
        equipment: item,
      } satisfies EquipmentDialogData,
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.error.set(null);
      this.profileService
        .updateEquipment(this.profileId, item.id, {
          name: result.name,
          compatibleSlotIds: result.compatibleSlotIds,
          stats: result.stats,
        })
        .subscribe({
          next: updated => {
            this.equipmentChanged.emit(
              this.equipment.map(e => (e.id === updated.id ? updated : e)),
            );
          },
          error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to update equipment.'),
        });
    });
  }

  duplicateEquipment(item: EquipmentDto): void {
    const newName = this.generateUniqueName(item.name);

    this.error.set(null);
    this.profileService
      .createEquipment(this.profileId, {
        name: newName,
        compatibleSlotIds: [...item.compatibleSlotIds],
        stats: item.stats.map(s => ({ statTypeId: s.statTypeId, value: s.value })),
      })
      .subscribe({
        next: eq => this.equipmentChanged.emit([...this.equipment, eq]),
        error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to duplicate equipment.'),
      });
  }

  deleteEquipment(item: EquipmentDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '380px',
      data: {
        title: 'Delete Equipment',
        message: `Delete "${item.name}"?`,
        confirmText: 'Delete',
        warn: true,
      } satisfies ConfirmDialogData,
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.error.set(null);
      this.profileService.deleteEquipment(this.profileId, item.id).subscribe({
        next: () => this.equipmentChanged.emit(this.equipment.filter(e => e.id !== item.id)),
        error: () => this.error.set('Failed to delete equipment.'),
      });
    });
  }

  getSlotName(slotId: number): string {
    return this.slots.find(s => s.id === slotId)?.name ?? 'Unknown';
  }

  /** Returns a short summary of slot names, truncating after 2. */
  getSlotSummary(item: EquipmentDto): string {
    const names = item.compatibleSlotIds.map(id => this.getSlotName(id));
    if (names.length <= 2) return names.join(', ');
    return `${names[0]}, ${names[1]} +${names.length - 2} more`;
  }

  /** Returns all slot names joined, used for tooltip on truncated lists. */
  getAllSlotNames(item: EquipmentDto): string {
    return item.compatibleSlotIds.map(id => this.getSlotName(id)).join(', ');
  }

  getStatDisplay(stat: { statTypeId: number; value: number }): string {
    const st = this.statTypes.find(s => s.id === stat.statTypeId);
    return `${st?.displayName ?? 'Unknown'}: ${stat.value}`;
  }

  /** Generate a unique name by appending an incrementing number. */
  private generateUniqueName(baseName: string): string {
    const existingNames = new Set(this.equipment.map(e => e.name));

    // Strip trailing number suffix if present (e.g., "Helmet 2" -> "Helmet")
    const match = baseName.match(/^(.+?)\s+(\d+)$/);
    const root = match ? match[1] : baseName;

    let counter = 2;
    let candidate = `${root} ${counter}`;
    while (existingNames.has(candidate)) {
      counter++;
      candidate = `${root} ${counter}`;
    }
    return candidate;
  }

  constructor(
    private readonly profileService: ProfileService,
    private readonly dialog: MatDialog,
  ) {}
}
