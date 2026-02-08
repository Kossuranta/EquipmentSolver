import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProfileService } from '../../services/profile.service';
import { ProfileDetailResponse } from '../../models/profile.models';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-profile-general-tab',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DatePipe,
  ],
  templateUrl: './profile-general-tab.component.html',
  styleUrl: './profile-general-tab.component.scss',
})
export class ProfileGeneralTabComponent {
  @Input({ required: true }) profile!: ProfileDetailResponse;
  @Input({ required: true }) isOwner!: boolean;
  @Output() profileUpdated = new EventEmitter<void>();

  editing = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);

  form = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.maxLength(100)]),
    description: new FormControl('', [Validators.maxLength(500)]),
  });

  startEditing(): void {
    this.form.patchValue({
      name: this.profile.name,
      description: this.profile.description ?? '',
    });
    this.editing.set(true);
  }

  cancelEditing(): void {
    this.editing.set(false);
    this.error.set(null);
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    this.profileService
      .updateProfile(this.profile.id, {
        name: this.form.value.name!,
        gameName: this.profile.gameName,
        igdbGameId: this.profile.igdbGameId,
        gameCoverUrl: this.profile.gameCoverUrl,
        description: this.form.value.description || null,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.editing.set(false);
          this.profileUpdated.emit();
        },
        error: err => {
          this.saving.set(false);
          this.error.set(err.error?.errors?.[0] ?? 'Failed to save.');
        },
      });
  }

  constructor(private readonly profileService: ProfileService) {}
}
