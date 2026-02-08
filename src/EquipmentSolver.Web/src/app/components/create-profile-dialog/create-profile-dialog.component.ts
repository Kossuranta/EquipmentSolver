import { Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { debounceTime, distinctUntilChanged, switchMap, of, catchError } from 'rxjs';
import { GameService } from '../../services/game.service';
import { ProfileService } from '../../services/profile.service';
import { GameSearchResult } from '../../models/profile.models';

@Component({
  selector: 'app-create-profile-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './create-profile-dialog.component.html',
  styleUrl: './create-profile-dialog.component.scss',
})
export class CreateProfileDialogComponent {
  gameSearchControl = new FormControl('');
  form = new FormGroup({
    description: new FormControl('', [Validators.maxLength(500)]),
  });

  gameResults = signal<GameSearchResult[]>([]);
  selectedGame = signal<GameSearchResult | null>(null);
  searching = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);

  constructor(
    private readonly dialogRef: MatDialogRef<CreateProfileDialogComponent>,
    private readonly gameService: GameService,
    private readonly profileService: ProfileService,
  ) {
    this.gameSearchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap(query => {
          if (!query || typeof query !== 'string' || query.length < 2) {
            this.gameResults.set([]);
            return of([]);
          }
          this.searching.set(true);
          return this.gameService.searchGames(query).pipe(
            catchError(() => {
              this.error.set('Failed to search games. Check API credentials.');
              return of([]);
            }),
          );
        }),
      )
      .subscribe(results => {
        this.gameResults.set(results);
        this.searching.set(false);
      });
  }

  displayGame(game: GameSearchResult): string {
    return game?.name ?? '';
  }

  selectGame(game: GameSearchResult): void {
    this.selectedGame.set(game);
    this.error.set(null);
  }

  save(): void {
    const game = this.selectedGame();
    if (!game) {
      this.error.set('Please select a game from the search results.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.profileService
      .createProfile({
        gameName: game.name,
        igdbGameId: game.igdbId,
        gameCoverUrl: game.coverUrl,
        description: this.form.value.description || null,
      })
      .subscribe({
        next: profile => {
          this.saving.set(false);
          this.dialogRef.close(profile);
        },
        error: err => {
          this.saving.set(false);
          this.error.set(err.error?.errors?.[0] ?? 'Failed to create profile.');
        },
      });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
