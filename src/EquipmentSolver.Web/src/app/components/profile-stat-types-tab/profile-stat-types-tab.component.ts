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
    name: new FormControl('', [
      Validators.required,
      Validators.maxLength(50),
      Validators.pattern(/^[a-z][a-z0-9_]*$/),
    ]),
    displayName: new FormControl('', [Validators.required, Validators.maxLength(100)]),
  });

  editingId = signal<number | null>(null);
  editForm = new FormGroup({
    name: new FormControl('', [
      Validators.required,
      Validators.maxLength(50),
      Validators.pattern(/^[a-z][a-z0-9_]*$/),
    ]),
    displayName: new FormControl('', [Validators.required, Validators.maxLength(100)]),
  });

  error = signal<string | null>(null);
  displayedColumns = ['name', 'displayName', 'actions'];

  addStatType(): void {
    if (this.addForm.invalid) return;

    const { name, displayName } = this.addForm.value;
    this.error.set(null);

    this.profileService
      .createStatType(this.profileId, { name: name!, displayName: displayName! })
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
    this.editForm.patchValue({ name: st.name, displayName: st.displayName });
  }

  cancelEditing(): void {
    this.editingId.set(null);
  }

  saveStatType(st: StatTypeDto): void {
    if (this.editForm.invalid) return;

    const { name, displayName } = this.editForm.value;
    this.error.set(null);

    this.profileService
      .updateStatType(this.profileId, st.id, { name: name!, displayName: displayName! })
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

  /// Auto-generate internal name from display name
  autoName(): void {
    const display = this.addForm.value.displayName ?? '';
    const auto = display
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '_')
      .replace(/^_|_$/g, '');
    if (!this.addForm.value.name) {
      this.addForm.patchValue({ name: auto });
    }
  }

  constructor(private readonly profileService: ProfileService) {}
}
