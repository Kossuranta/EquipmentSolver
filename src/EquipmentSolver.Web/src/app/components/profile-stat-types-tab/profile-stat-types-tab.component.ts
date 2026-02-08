import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { ProfileService } from '../../services/profile.service';
import { StatTypeDto } from '../../models/profile.models';

@Component({
  selector: 'app-profile-stat-types-tab',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
  ],
  templateUrl: './profile-stat-types-tab.component.html',
  styleUrl: './profile-stat-types-tab.component.scss',
})
export class ProfileStatTypesTabComponent {
  @Input({ required: true }) profileId!: number;
  @Input({ required: true }) statTypes!: StatTypeDto[];
  @Input({ required: true }) isOwner!: boolean;
  @Output() statTypesChanged = new EventEmitter<StatTypeDto[]>();

  addForm = new FormGroup({
    displayName: new FormControl('', [Validators.required, Validators.maxLength(200)]),
  });

  editingId = signal<number | null>(null);
  editForm = new FormGroup({
    displayName: new FormControl('', [Validators.required, Validators.maxLength(200)]),
  });

  error = signal<string | null>(null);
  displayedColumns = ['displayName', 'actions'];

  addStatType(): void {
    if (this.addForm.invalid) return;

    const { displayName } = this.addForm.value;
    this.error.set(null);

    this.profileService
      .createStatType(this.profileId, { displayName: displayName! })
      .subscribe({
        next: statType => {
          this.statTypesChanged.emit([...this.statTypes, statType]);
          this.addForm.reset();
        },
        error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to add stat type.'),
      });
  }

  startEditing(st: StatTypeDto): void {
    this.editingId.set(st.id);
    this.editForm.patchValue({ displayName: st.displayName });
  }

  cancelEditing(): void {
    this.editingId.set(null);
  }

  saveStatType(st: StatTypeDto): void {
    if (this.editForm.invalid) return;

    const { displayName } = this.editForm.value;
    this.error.set(null);

    this.profileService
      .updateStatType(this.profileId, st.id, { displayName: displayName! })
      .subscribe({
        next: updated => {
          const newList = this.statTypes.map(s => (s.id === updated.id ? updated : s));
          this.statTypesChanged.emit(newList);
          this.editingId.set(null);
        },
        error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to update.'),
      });
  }

  deleteStatType(st: StatTypeDto): void {
    if (
      !confirm(
        `Delete stat type "${st.displayName}"? This will remove it from all equipment and solver presets.`,
      )
    )
      return;

    this.error.set(null);
    this.profileService.deleteStatType(this.profileId, st.id).subscribe({
      next: () => {
        this.statTypesChanged.emit(this.statTypes.filter(s => s.id !== st.id));
      },
      error: () => this.error.set('Failed to delete stat type.'),
    });
  }

  constructor(private readonly profileService: ProfileService) {}
}
