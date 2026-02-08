import { Component, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProfileService } from '../../services/profile.service';
import {
  ProfileDetailResponse,
  SlotDto,
  StatTypeDto,
  EquipmentDto,
} from '../../models/profile.models';
import { ProfileGeneralTabComponent } from '../../components/profile-general-tab/profile-general-tab.component';
import { ProfileSlotsTabComponent } from '../../components/profile-slots-tab/profile-slots-tab.component';
import { ProfileStatTypesTabComponent } from '../../components/profile-stat-types-tab/profile-stat-types-tab.component';
import { ProfileEquipmentTabComponent } from '../../components/profile-equipment-tab/profile-equipment-tab.component';
import { ProfilePatchNotesTabComponent } from '../../components/profile-patch-notes-tab/profile-patch-notes-tab.component';
import { ProfileUserSelectionTabComponent } from '../../components/profile-user-selection-tab/profile-user-selection-tab.component';

@Component({
  selector: 'app-profile-editor',
  imports: [
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    ProfileGeneralTabComponent,
    ProfileSlotsTabComponent,
    ProfileStatTypesTabComponent,
    ProfileEquipmentTabComponent,
    ProfilePatchNotesTabComponent,
    ProfileUserSelectionTabComponent,
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
}
