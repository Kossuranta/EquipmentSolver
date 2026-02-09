import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../services/auth.service';
import { ProfileService } from '../../services/profile.service';
import { BrowseService } from '../../services/browse.service';
import { NotificationService } from '../../services/notification.service';
import { ProfileResponse, ProfileExportData } from '../../models/profile.models';
import { CreateProfileDialogComponent } from '../../components/create-profile-dialog/create-profile-dialog.component';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../components/confirm-dialog/confirm-dialog.component';
import { readFileAsText } from '../../utils/csv.utils';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatTooltipModule,
    DatePipe,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage implements OnInit {
  profiles = signal<ProfileResponse[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor(
    readonly authService: AuthService,
    private readonly profileService: ProfileService,
    private readonly browseService: BrowseService,
    private readonly notify: NotificationService,
    private readonly router: Router,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadProfiles();
  }

  loadProfiles(): void {
    this.loading.set(true);
    this.error.set(null);
    this.profileService.getMyProfiles().subscribe({
      next: profiles => {
        this.profiles.set(profiles);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load profiles.');
        this.loading.set(false);
      },
    });
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(CreateProfileDialogComponent, {
      width: '500px',
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.router.navigate(['/profiles', result.id]);
      }
    });
  }

  openProfile(profile: ProfileResponse): void {
    this.router.navigate(['/profiles', profile.id]);
  }

  deleteProfile(event: Event, profile: ProfileResponse): void {
    event.stopPropagation();

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Profile',
        message: `Delete profile "${profile.name}"? This cannot be undone.`,
        confirmText: 'Delete',
        warn: true,
      } satisfies ConfirmDialogData,
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.profileService.deleteProfile(profile.id).subscribe({
        next: () => this.loadProfiles(),
        error: () => this.error.set('Failed to delete profile.'),
      });
    });
  }

  importProfile(): void {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json,application/json';
    input.onchange = () => {
      const file = input.files?.[0];
      if (!file) return;
      this.processImportFile(file);
    };
    input.click();
  }

  private async processImportFile(file: File): Promise<void> {
    try {
      const text = await readFileAsText(file);
      const data = JSON.parse(text) as ProfileExportData;

      if (!data.profile?.name) {
        this.error.set('Invalid profile JSON: missing profile name.');
        return;
      }

      this.profileService.importProfileAsNew(data).subscribe({
        next: result => {
          this.notify.success(`Profile "${result.name}" imported successfully.`);
          this.router.navigate(['/profiles', result.id]);
        },
        error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to import profile.'),
      });
    } catch {
      this.error.set('Failed to parse JSON file.');
    }
  }

  stopUsing(event: Event, profile: ProfileResponse): void {
    event.stopPropagation();

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '380px',
      data: {
        title: 'Stop Using Profile',
        message: `Stop using "${profile.name}" by ${profile.ownerName}?`,
        confirmText: 'Stop Using',
      } satisfies ConfirmDialogData,
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.browseService.stopUsing(profile.id).subscribe({
        next: () => this.loadProfiles(),
        error: () => this.error.set('Failed to stop using profile.'),
      });
    });
  }
}
