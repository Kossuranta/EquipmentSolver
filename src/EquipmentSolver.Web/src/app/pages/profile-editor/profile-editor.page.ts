import { Component, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog } from '@angular/material/dialog';
import { ProfileService } from '../../services/profile.service';
import { BrowseService } from '../../services/browse.service';
import { NotificationService } from '../../services/notification.service';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../components/confirm-dialog/confirm-dialog.component';
import {
  ProfileImportDialogComponent,
  ProfileImportDialogData,
  ProfileImportDialogResult,
} from '../../components/profile-import-dialog/profile-import-dialog.component';
import {
  ProfileDetailResponse,
  SlotDto,
  StatTypeDto,
  EquipmentDto,
  ProfileExportData,
} from '../../models/profile.models';
import { downloadFile, readFileAsText } from '../../utils/csv.utils';
import { ProfileGeneralTabComponent } from '../../components/profile-general-tab/profile-general-tab.component';
import { ProfileSlotsTabComponent } from '../../components/profile-slots-tab/profile-slots-tab.component';
import { ProfileStatTypesTabComponent } from '../../components/profile-stat-types-tab/profile-stat-types-tab.component';
import { ProfileEquipmentTabComponent } from '../../components/profile-equipment-tab/profile-equipment-tab.component';
import { ProfilePatchNotesTabComponent } from '../../components/profile-patch-notes-tab/profile-patch-notes-tab.component';
import { ProfileUserSelectionTabComponent } from '../../components/profile-user-selection-tab/profile-user-selection-tab.component';
import { ProfileSolverTabComponent } from '../../components/profile-solver-tab/profile-solver-tab.component';

@Component({
  selector: 'app-profile-editor',
  imports: [
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatMenuModule,
    ProfileGeneralTabComponent,
    ProfileSlotsTabComponent,
    ProfileStatTypesTabComponent,
    ProfileEquipmentTabComponent,
    ProfilePatchNotesTabComponent,
    ProfileUserSelectionTabComponent,
    ProfileSolverTabComponent,
  ],
  templateUrl: './profile-editor.page.html',
  styleUrl: './profile-editor.page.scss',
})
export class ProfileEditorPage implements OnInit {
  profile = signal<ProfileDetailResponse | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  profileId = 0;

  slots = computed(() => this.profile()?.slots ?? []);
  statTypes = computed(() => this.profile()?.statTypes ?? []);
  equipment = computed(() => this.profile()?.equipment ?? []);
  isOwner = computed(() => this.profile()?.isOwner ?? false);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly profileService: ProfileService,
    private readonly browseService: BrowseService,
    private readonly dialog: MatDialog,
    private readonly notify: NotificationService,
  ) {}

  ngOnInit(): void {
    this.profileId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);
    this.profileService.getProfile(this.profileId).subscribe({
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

  onProfileUpdated(): void {
    this.loadProfile();
  }

  onSlotsChanged(slots: SlotDto[]): void {
    const current = this.profile();
    if (current) {
      this.profile.set({ ...current, slots });
    }
  }

  onStatTypesChanged(statTypes: StatTypeDto[]): void {
    const current = this.profile();
    if (current) {
      this.profile.set({ ...current, statTypes });
    }
  }

  onEquipmentChanged(equipment: EquipmentDto[]): void {
    const current = this.profile();
    if (current) {
      this.profile.set({ ...current, equipment });
    }
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  exportProfileJson(): void {
    this.profileService.exportProfile(this.profileId).subscribe({
      next: blob => {
        const name = this.profile()?.name ?? 'profile';
        downloadFile(blob, `${name}.json`, 'application/json');
      },
      error: () => this.error.set('Failed to export profile.'),
    });
  }

  openReplaceDialog(): void {
    const p = this.profile();
    if (!p) return;

    const dialogRef = this.dialog.open(ProfileImportDialogComponent, {
      width: '550px',
      data: {
        profileId: this.profileId,
        profileName: p.name,
      } satisfies ProfileImportDialogData,
    });

    dialogRef.afterClosed().subscribe((result: ProfileImportDialogResult | undefined) => {
      if (!result) return;
      this.profileService.replaceProfile(this.profileId, result.data).subscribe({
        next: () => {
          this.notify.success('Profile replaced successfully.');
          this.loadProfile();
        },
        error: err => this.error.set(err.error?.errors?.[0] ?? 'Failed to replace profile.'),
      });
    });
  }

  stopUsing(): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '380px',
      data: {
        title: 'Stop Using Profile',
        message: `Stop using "${this.profile()?.name}"?`,
        confirmText: 'Stop Using',
      } satisfies ConfirmDialogData,
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.browseService.stopUsing(this.profileId).subscribe({
        next: () => this.router.navigate(['/dashboard']),
        error: () => this.error.set('Failed to stop using profile.'),
      });
    });
  }
}
