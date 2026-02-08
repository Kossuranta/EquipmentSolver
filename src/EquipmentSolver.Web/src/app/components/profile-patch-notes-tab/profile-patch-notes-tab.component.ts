import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { ProfileService } from '../../services/profile.service';
import { PatchNoteResponse } from '../../models/profile.models';
import { ConfirmDialogComponent, ConfirmDialogData } from '../confirm-dialog/confirm-dialog.component';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-profile-patch-notes-tab',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    DatePipe,
  ],
  templateUrl: './profile-patch-notes-tab.component.html',
  styleUrl: './profile-patch-notes-tab.component.scss',
})
export class ProfilePatchNotesTabComponent implements OnInit {
  @Input({ required: true }) profileId!: number;
  @Input({ required: true }) currentVersion!: string;
  @Input({ required: true }) isOwner!: boolean;
  @Output() versionBumped = new EventEmitter<void>();

  patchNotes = signal<PatchNoteResponse[]>([]);
  loading = signal(true);
  showForm = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);

  form = new FormGroup({
    major: new FormControl(0, [Validators.required, Validators.min(0), Validators.max(999)]),
    minor: new FormControl(1, [Validators.required, Validators.min(0), Validators.max(999)]),
    patch: new FormControl(0, [Validators.required, Validators.min(0), Validators.max(999)]),
    content: new FormControl('', [Validators.required, Validators.maxLength(5000)]),
  });

  constructor(
    private readonly profileService: ProfileService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadPatchNotes();
  }

  loadPatchNotes(): void {
    this.loading.set(true);
    this.profileService.getPatchNotes(this.profileId).subscribe({
      next: notes => {
        this.patchNotes.set(notes);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load patch notes.');
        this.loading.set(false);
      },
    });
  }

  openForm(): void {
    // Pre-fill with current version, auto-bump patch
    const parts = this.currentVersion.split('.').map(Number);
    this.form.patchValue({
      major: parts[0] ?? 0,
      minor: parts[1] ?? 1,
      patch: (parts[2] ?? 0) + 1,
      content: '',
    });
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.error.set(null);
  }

  save(): void {
    if (this.form.invalid) return;

    const { major, minor, patch, content } = this.form.value;
    const version = `${major}.${minor}.${patch}`;

    this.saving.set(true);
    this.error.set(null);

    this.profileService
      .createPatchNote(this.profileId, { version, content: content! })
      .subscribe({
        next: note => {
          this.patchNotes.update(notes => [note, ...notes]);
          this.saving.set(false);
          this.showForm.set(false);
          this.versionBumped.emit();
        },
        error: err => {
          this.saving.set(false);
          this.error.set(err.error?.errors?.[0] ?? 'Failed to save patch note.');
        },
      });
  }

  deleteNote(note: PatchNoteResponse): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '380px',
      data: {
        title: 'Delete Patch Note',
        message: 'Delete this patch note?',
        confirmText: 'Delete',
        warn: true,
      } satisfies ConfirmDialogData,
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.profileService.deletePatchNote(this.profileId, note.id).subscribe({
        next: () => {
          this.patchNotes.update(notes => notes.filter(n => n.id !== note.id));
        },
        error: () => this.error.set('Failed to delete patch note.'),
      });
    });
  }
}
