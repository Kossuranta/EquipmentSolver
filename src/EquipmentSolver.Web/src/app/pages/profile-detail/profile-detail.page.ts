import { Component, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DatePipe } from '@angular/common';
import { BrowseService } from '../../services/browse.service';
import { PublicProfileDetailResponse } from '../../models/profile.models';

@Component({
  selector: 'app-profile-detail',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatTabsModule,
    MatExpansionModule,
    MatTooltipModule,
    MatDividerModule,
    MatSnackBarModule,
    DatePipe,
  ],
  templateUrl: './profile-detail.page.html',
  styleUrl: './profile-detail.page.scss',
})
export class ProfileDetailPage implements OnInit {
  profile = signal<PublicProfileDetailResponse | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  profileId = 0;

  slotNames = computed(() => {
    const p = this.profile();
    if (!p) return new Map<number, string>();
    return new Map(p.slots.map(s => [s.id, s.name]));
  });

  statNames = computed(() => {
    const p = this.profile();
    if (!p) return new Map<number, string>();
    return new Map(p.statTypes.map(st => [st.id, st.displayName]));
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly browseService: BrowseService,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.profileId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);
    this.browseService.getPublicProfile(this.profileId).subscribe({
      next: profile => {
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load profile.');
        this.loading.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/browse']);
  }

  vote(vote: number): void {
    const p = this.profile();
    if (!p) return;
    const newVote = p.userVote === vote ? 0 : vote;

    this.browseService.vote(this.profileId, newVote).subscribe({
      next: response => {
        this.profile.set({
          ...p,
          voteScore: response.newScore,
          userVote: response.userVote === 0 ? null : response.userVote,
        });
      },
      error: err => {
        this.snackBar.open(err.error?.errors?.[0] ?? 'Failed to vote.', 'OK', { duration: 3000 });
      },
    });
  }

  startUsing(): void {
    const p = this.profile();
    if (!p) return;

    this.browseService.startUsing(this.profileId).subscribe({
      next: () => {
        this.profile.set({ ...p, isUsing: true, usageCount: p.usageCount + 1 });
        this.snackBar.open('Profile added to your dashboard.', 'OK', { duration: 3000 });
      },
      error: err => {
        this.snackBar.open(err.error?.errors?.[0] ?? 'Failed to use profile.', 'OK', { duration: 3000 });
      },
    });
  }

  stopUsing(): void {
    const p = this.profile();
    if (!p) return;

    this.browseService.stopUsing(this.profileId).subscribe({
      next: () => {
        this.profile.set({ ...p, isUsing: false, usageCount: Math.max(0, p.usageCount - 1) });
        this.snackBar.open('Profile removed from your dashboard.', 'OK', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to stop using profile.', 'OK', { duration: 3000 });
      },
    });
  }

  copyProfile(): void {
    this.browseService.copyProfile(this.profileId).subscribe({
      next: result => {
        this.snackBar.open(`Copied as "${result.name}". Opening...`, 'OK', { duration: 3000 });
        this.router.navigate(['/profiles', result.id]);
      },
      error: () => {
        this.snackBar.open('Failed to copy profile.', 'OK', { duration: 3000 });
      },
    });
  }

  openInEditor(): void {
    this.router.navigate(['/profiles', this.profileId]);
  }

  getSlotName(slotId: number): string {
    return this.slotNames().get(slotId) ?? 'Unknown';
  }

  getStatName(statTypeId: number): string {
    return this.statNames().get(statTypeId) ?? 'Unknown';
  }

  getCompatibleSlots(slotIds: number[]): string {
    return slotIds.map(id => this.getSlotName(id)).join(', ');
  }
}
