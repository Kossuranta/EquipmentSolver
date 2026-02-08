import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDialog } from '@angular/material/dialog';
import { ProfileService } from '../../services/profile.service';
import { SlotDto } from '../../models/profile.models';
import { ConfirmDialogComponent, ConfirmDialogData } from '../confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-profile-slots-tab',
  imports: [
    ReactiveFormsModule,
    DragDropModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
  ],
  templateUrl: './profile-slots-tab.component.html',
  styleUrl: './profile-slots-tab.component.scss',
})
export class ProfileSlotsTabComponent {
  @Input({ required: true }) profileId!: number;
  @Input({ required: true }) slots!: SlotDto[];
  @Input({ required: true }) isOwner!: boolean;
  @Output() slotsChanged = new EventEmitter<SlotDto[]>();

  newSlotName = new FormControl('', [Validators.required, Validators.maxLength(100)]);
  editingSlotId = signal<number | null>(null);
  editSlotName = new FormControl('', [Validators.required, Validators.maxLength(100)]);
  error = signal<string | null>(null);

  addSlot(): void {
    const name = this.newSlotName.value?.trim();
    if (!name) return;

    this.error.set(null);
    this.profileService.createSlot(this.profileId, { name }).subscribe({
      next: slot => {
        this.slotsChanged.emit([...this.slots, slot]);
        this.newSlotName.reset();
      },
      error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to add slot.'),
    });
  }

  startEditing(slot: SlotDto): void {
    this.editingSlotId.set(slot.id);
    this.editSlotName.setValue(slot.name);
  }

  cancelEditing(): void {
    this.editingSlotId.set(null);
  }

  saveSlot(slot: SlotDto): void {
    const name = this.editSlotName.value?.trim();
    if (!name) return;

    this.error.set(null);
    this.profileService.updateSlot(this.profileId, slot.id, { name }).subscribe({
      next: updated => {
        const newSlots = this.slots.map(s => (s.id === updated.id ? updated : s));
        this.slotsChanged.emit(newSlots);
        this.editingSlotId.set(null);
      },
      error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to update slot.'),
    });
  }

  deleteSlot(slot: SlotDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Delete Slot',
        message: `Delete slot "${slot.name}"? Equipment assigned to this slot will lose this assignment.`,
        confirmText: 'Delete',
        warn: true,
      } satisfies ConfirmDialogData,
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.error.set(null);
      this.profileService.deleteSlot(this.profileId, slot.id).subscribe({
        next: () => {
          this.slotsChanged.emit(this.slots.filter(s => s.id !== slot.id));
        },
        error: () => this.error.set('Failed to delete slot.'),
      });
    });
  }

  onDrop(event: CdkDragDrop<SlotDto[]>): void {
    const reordered = [...this.slots];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);

    // Optimistic update
    this.slotsChanged.emit(reordered);

    const slotIds = reordered.map(s => s.id);
    this.profileService.reorderSlots(this.profileId, { slotIds }).subscribe({
      next: updated => this.slotsChanged.emit(updated),
      error: () => {
        // Revert on failure
        this.slotsChanged.emit(this.slots);
        this.error.set('Failed to reorder slots.');
      },
    });
  }

  constructor(
    private readonly profileService: ProfileService,
    private readonly dialog: MatDialog,
  ) {}
}
