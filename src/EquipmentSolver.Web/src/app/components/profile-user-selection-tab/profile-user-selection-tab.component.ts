import { Component, Input, OnInit, signal, computed } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { ProfileService } from '../../services/profile.service';
import { EquipmentDto, SlotDto, StatTypeDto } from '../../models/profile.models';

@Component({
  selector: 'app-profile-user-selection-tab',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatDividerModule,
  ],
  templateUrl: './profile-user-selection-tab.component.html',
  styleUrl: './profile-user-selection-tab.component.scss',
})
export class ProfileUserSelectionTabComponent implements OnInit {
  @Input({ required: true }) profileId!: number;
  @Input({ required: true }) equipment!: EquipmentDto[];
  @Input({ required: true }) slots!: SlotDto[];
  @Input({ required: true }) statTypes!: StatTypeDto[];

  equipmentStates = signal<Map<number, boolean>>(new Map());
  slotStates = signal<Map<number, boolean>>(new Map());
  loading = signal(true);
  error = signal<string | null>(null);

  enabledCount = computed(() => {
    const states = this.equipmentStates();
    return this.equipment.filter(e => states.get(e.id) ?? true).length;
  });

  constructor(private readonly profileService: ProfileService) {}

  ngOnInit(): void {
    this.loadStates();
  }

  loadStates(): void {
    this.loading.set(true);
    let loaded = 0;
    const checkDone = () => {
      loaded++;
      if (loaded >= 2) this.loading.set(false);
    };

    this.profileService.getEquipmentStates(this.profileId).subscribe({
      next: states => {
        const map = new Map<number, boolean>();
        for (const s of states) map.set(s.equipmentId, s.isEnabled);
        this.equipmentStates.set(map);
        checkDone();
      },
      error: () => {
        this.error.set('Failed to load equipment states.');
        checkDone();
      },
    });

    this.profileService.getSlotStates(this.profileId).subscribe({
      next: states => {
        const map = new Map<number, boolean>();
        for (const s of states) map.set(s.slotId, s.isEnabled);
        this.slotStates.set(map);
        checkDone();
      },
      error: () => {
        this.error.set('Failed to load slot states.');
        checkDone();
      },
    });
  }

  isEquipmentEnabled(equipmentId: number): boolean {
    return this.equipmentStates().get(equipmentId) ?? true;
  }

  isSlotEnabled(slotId: number): boolean {
    return this.slotStates().get(slotId) ?? true;
  }

  toggleEquipment(equipmentId: number): void {
    const current = this.isEquipmentEnabled(equipmentId);
    const newState = !current;

    // Optimistic update
    const map = new Map(this.equipmentStates());
    map.set(equipmentId, newState);
    this.equipmentStates.set(map);

    this.profileService.setEquipmentState(this.profileId, equipmentId, newState).subscribe({
      error: () => {
        // Revert
        const revert = new Map(this.equipmentStates());
        revert.set(equipmentId, current);
        this.equipmentStates.set(revert);
        this.error.set('Failed to update equipment state.');
      },
    });
  }

  toggleSlot(slotId: number): void {
    const current = this.isSlotEnabled(slotId);
    const newState = !current;

    const map = new Map(this.slotStates());
    map.set(slotId, newState);
    this.slotStates.set(map);

    this.profileService.setSlotState(this.profileId, slotId, newState).subscribe({
      error: () => {
        const revert = new Map(this.slotStates());
        revert.set(slotId, current);
        this.slotStates.set(revert);
        this.error.set('Failed to update slot state.');
      },
    });
  }

  enableAll(): void {
    this.profileService.bulkSetEquipmentState(this.profileId, true).subscribe({
      next: () => {
        const map = new Map<number, boolean>();
        for (const e of this.equipment) map.set(e.id, true);
        this.equipmentStates.set(map);
      },
      error: () => this.error.set('Failed to enable all.'),
    });
  }

  disableAll(): void {
    this.profileService.bulkSetEquipmentState(this.profileId, false).subscribe({
      next: () => {
        const map = new Map<number, boolean>();
        for (const e of this.equipment) map.set(e.id, false);
        this.equipmentStates.set(map);
      },
      error: () => this.error.set('Failed to disable all.'),
    });
  }

  getSlotName(slotId: number): string {
    return this.slots.find(s => s.id === slotId)?.name ?? 'Unknown';
  }

  getStatDisplay(stat: { statTypeId: number; value: number }): string {
    const st = this.statTypes.find(s => s.id === stat.statTypeId);
    return `${st?.displayName ?? '?'}: ${stat.value}`;
  }

  getEquipmentForSlot(slotId: number): EquipmentDto[] {
    return this.equipment.filter(e => e.compatibleSlotIds.includes(slotId));
  }
}
