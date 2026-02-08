import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../services/auth.service';
import { ProfileService } from '../../services/profile.service';
import { ProfileResponse } from '../../models/profile.models';
import { CreateProfileDialogComponent } from '../../components/create-profile-dialog/create-profile-dialog.component';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
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
    if (!confirm(`Delete profile for "${profile.gameName}"? This cannot be undone.`)) return;

    this.profileService.deleteProfile(profile.id).subscribe({
      next: () => this.loadProfiles(),
      error: () => this.error.set('Failed to delete profile.'),
    });
  }
}
