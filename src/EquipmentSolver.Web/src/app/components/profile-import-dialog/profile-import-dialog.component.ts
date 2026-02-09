import { Component, Inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { ProfileExportData } from '../../models/profile.models';
import { readFileAsText, downloadFile } from '../../utils/csv.utils';
import { ProfileService } from '../../services/profile.service';

export interface ProfileImportDialogData {
  profileId: number;
  profileName: string;
}

export interface ProfileImportDialogResult {
  data: ProfileExportData;
}

@Component({
  selector: 'app-profile-import-dialog',
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
  ],
  templateUrl: './profile-import-dialog.component.html',
  styleUrl: './profile-import-dialog.component.scss',
})
export class ProfileImportDialogComponent {
  selectedFile = signal<File | null>(null);
  parsedData = signal<ProfileExportData | null>(null);
  parseError = signal<string | null>(null);

  constructor(
    private readonly dialogRef: MatDialogRef<ProfileImportDialogComponent>,
    @Inject(MAT_DIALOG_DATA) readonly data: ProfileImportDialogData,
    private readonly profileService: ProfileService,
  ) {}

  exportCurrentVersion(): void {
    this.profileService.exportProfile(this.data.profileId).subscribe({
      next: blob => downloadFile(blob, `${this.data.profileName}.json`, 'application/json'),
      error: () => this.parseError.set('Failed to export current version.'),
    });
  }

  selectFile(): void {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json,application/json';
    input.onchange = () => {
      const file = input.files?.[0];
      if (!file) return;
      this.processFile(file);
    };
    input.click();
  }

  private async processFile(file: File): Promise<void> {
    this.parseError.set(null);
    this.selectedFile.set(file);

    try {
      const text = await readFileAsText(file);
      const data = JSON.parse(text) as ProfileExportData;

      if (!data.profile?.name) {
        this.parseError.set('Invalid profile JSON: missing profile name.');
        this.parsedData.set(null);
        return;
      }

      this.parsedData.set(data);
    } catch {
      this.parseError.set('Failed to parse JSON file.');
      this.parsedData.set(null);
    }
  }

  submit(): void {
    const data = this.parsedData();
    if (!data) return;
    this.dialogRef.close({ data } satisfies ProfileImportDialogResult);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
