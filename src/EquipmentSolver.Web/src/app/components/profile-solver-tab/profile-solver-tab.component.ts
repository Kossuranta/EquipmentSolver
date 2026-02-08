import { Component, Input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { ProfileService } from '../../services/profile.service';
import {
  StatTypeDto,
  SolveConstraintInput,
  SolvePriorityInput,
  SolveResponse,
  SolveResultDto,
  PresetResponse,
} from '../../models/profile.models';

interface ConstraintRow {
  statTypeId: number | null;
  operator: string;
  value: number;
}

interface PriorityRow {
  statTypeId: number | null;
  weight: number;
}

@Component({
  selector: 'app-profile-solver-tab',
  imports: [
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatExpansionModule,
    MatTooltipModule,
    MatChipsModule,
    MatMenuModule,
  ],
  templateUrl: './profile-solver-tab.component.html',
  styleUrl: './profile-solver-tab.component.scss',
})
export class ProfileSolverTabComponent implements OnInit {
  @Input({ required: true }) profileId!: number;
  @Input({ required: true }) statTypes!: StatTypeDto[];
  @Input({ required: true }) isOwner!: boolean;

  constraints = signal<ConstraintRow[]>([]);
  priorities = signal<PriorityRow[]>([{ statTypeId: null, weight: 1.0 }]);
  topN = signal(5);

  solving = signal(false);
  solveError = signal<string | null>(null);
  solveResponse = signal<SolveResponse | null>(null);

  presets = signal<PresetResponse[]>([]);
  presetsLoading = signal(false);
  presetName = signal('');
  presetError = signal<string | null>(null);
  selectedPresetId = signal<number | null>(null);

  operators = ['<=', '>=', '==', '<', '>'];

  constructor(private readonly profileService: ProfileService) {}

  ngOnInit(): void {
    this.loadPresets();
  }

  // --- Constraints ---

  addConstraint(): void {
    this.constraints.update(list => [...list, { statTypeId: null, operator: '<=', value: 0 }]);
  }

  removeConstraint(index: number): void {
    this.constraints.update(list => list.filter((_, i) => i !== index));
  }

  updateConstraint(index: number, field: keyof ConstraintRow, value: unknown): void {
    this.constraints.update(list =>
      list.map((row, i) => (i === index ? { ...row, [field]: value } : row)),
    );
  }

  // --- Priorities ---

  addPriority(): void {
    this.priorities.update(list => [...list, { statTypeId: null, weight: 1.0 }]);
  }

  removePriority(index: number): void {
    this.priorities.update(list => list.filter((_, i) => i !== index));
  }

  updatePriority(index: number, field: keyof PriorityRow, value: unknown): void {
    this.priorities.update(list =>
      list.map((row, i) => (i === index ? { ...row, [field]: value } : row)),
    );
  }

  // --- Available stats (not yet used in the current list) ---

  availableStatsForConstraint(currentIndex: number): StatTypeDto[] {
    const usedIds = new Set(
      this.constraints()
        .filter((_, i) => i !== currentIndex)
        .map(c => c.statTypeId)
        .filter((id): id is number => id !== null),
    );
    return this.statTypes.filter(st => !usedIds.has(st.id));
  }

  availableStatsForPriority(currentIndex: number): StatTypeDto[] {
    const usedIds = new Set(
      this.priorities()
        .filter((_, i) => i !== currentIndex)
        .map(p => p.statTypeId)
        .filter((id): id is number => id !== null),
    );
    return this.statTypes.filter(st => !usedIds.has(st.id));
  }

  // --- Solve ---

  canSolve(): boolean {
    const p = this.priorities();
    if (p.length === 0) return false;
    if (p.some(row => row.statTypeId === null || row.weight === 0)) return false;

    const c = this.constraints();
    if (c.some(row => row.statTypeId === null)) return false;

    return true;
  }

  solve(): void {
    if (!this.canSolve()) return;

    this.solving.set(true);
    this.solveError.set(null);
    this.solveResponse.set(null);

    const constraints: SolveConstraintInput[] = this.constraints()
      .filter((c): c is ConstraintRow & { statTypeId: number } => c.statTypeId !== null)
      .map(c => ({ statTypeId: c.statTypeId, operator: c.operator, value: c.value }));

    const priorities: SolvePriorityInput[] = this.priorities()
      .filter((p): p is PriorityRow & { statTypeId: number } => p.statTypeId !== null)
      .map(p => ({ statTypeId: p.statTypeId, weight: p.weight }));

    this.profileService
      .solve(this.profileId, { constraints, priorities, topN: this.topN() })
      .subscribe({
        next: response => {
          this.solveResponse.set(response);
          this.solving.set(false);
        },
        error: err => {
          const msg =
            err.error?.errors?.[0] ?? err.error?.title ?? 'Solver failed. Please try again.';
          this.solveError.set(msg);
          this.solving.set(false);
        },
      });
  }

  // --- Presets ---

  loadPresets(): void {
    this.presetsLoading.set(true);
    this.profileService.getPresets(this.profileId).subscribe({
      next: presets => {
        this.presets.set(presets);
        this.presetsLoading.set(false);
      },
      error: () => {
        this.presetError.set('Failed to load presets.');
        this.presetsLoading.set(false);
      },
    });
  }

  loadPreset(preset: PresetResponse): void {
    this.selectedPresetId.set(preset.id);
    this.presetName.set(preset.name);

    this.constraints.set(
      preset.constraints.map(c => ({
        statTypeId: c.statTypeId,
        operator: c.operator,
        value: c.value,
      })),
    );

    this.priorities.set(
      preset.priorities.map(p => ({
        statTypeId: p.statTypeId,
        weight: p.weight,
      })),
    );
  }

  savePreset(): void {
    const name = this.presetName().trim();
    if (!name) return;

    const constraints: SolveConstraintInput[] = this.constraints()
      .filter((c): c is ConstraintRow & { statTypeId: number } => c.statTypeId !== null)
      .map(c => ({ statTypeId: c.statTypeId, operator: c.operator, value: c.value }));

    const priorities: SolvePriorityInput[] = this.priorities()
      .filter((p): p is PriorityRow & { statTypeId: number } => p.statTypeId !== null)
      .map(p => ({ statTypeId: p.statTypeId, weight: p.weight }));

    if (priorities.length === 0) {
      this.presetError.set('Add at least one priority before saving.');
      return;
    }

    const selectedId = this.selectedPresetId();

    if (selectedId) {
      // Update existing
      this.profileService
        .updatePreset(this.profileId, selectedId, { name, constraints, priorities })
        .subscribe({
          next: updated => {
            this.presets.update(list =>
              list.map(p => (p.id === updated.id ? updated : p)),
            );
            this.presetError.set(null);
          },
          error: () => this.presetError.set('Failed to update preset.'),
        });
    } else {
      // Create new
      this.profileService
        .createPreset(this.profileId, { name, constraints, priorities })
        .subscribe({
          next: created => {
            this.presets.update(list => [...list, created]);
            this.selectedPresetId.set(created.id);
            this.presetError.set(null);
          },
          error: () => this.presetError.set('Failed to save preset.'),
        });
    }
  }

  deletePreset(presetId: number): void {
    this.profileService.deletePreset(this.profileId, presetId).subscribe({
      next: () => {
        this.presets.update(list => list.filter(p => p.id !== presetId));
        if (this.selectedPresetId() === presetId) {
          this.selectedPresetId.set(null);
          this.presetName.set('');
        }
      },
      error: () => this.presetError.set('Failed to delete preset.'),
    });
  }

  newPreset(): void {
    this.selectedPresetId.set(null);
    this.presetName.set('');
    this.constraints.set([]);
    this.priorities.set([{ statTypeId: null, weight: 1.0 }]);
  }

  // --- Helpers ---

  /** Convert comma to dot for decimal input (European locale support). */
  onDecimalKeydown(event: KeyboardEvent): void {
    if (event.key === ',') {
      event.preventDefault();
      document.execCommand('insertText', false, '.');
    }
  }

  getStatDisplayName(statTypeId: number): string {
    return this.statTypes.find(st => st.id === statTypeId)?.displayName ?? '?';
  }

  formatElapsed(ms: number): string {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  }
}
