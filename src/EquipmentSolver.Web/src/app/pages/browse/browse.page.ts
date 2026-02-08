import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged, switchMap, of, catchError } from 'rxjs';
import { BrowseService } from '../../services/browse.service';
import { GameService } from '../../services/game.service';
import { BrowseProfileItem, GameSearchResult } from '../../models/profile.models';

@Component({
  selector: 'app-browse',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatPaginatorModule,
    MatTooltipModule,
    MatSnackBarModule,
  ],
  templateUrl: './browse.page.html',
  styleUrl: './browse.page.scss',
})
export class BrowsePage implements OnInit {
  profiles = signal<BrowseProfileItem[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  totalCount = signal(0);
  page = signal(1);
  pageSize = signal(20);

  searchControl = new FormControl('');
  sortControl = new FormControl('votes');
  gameSearchControl = new FormControl('');

  gameResults = signal<GameSearchResult[]>([]);
  selectedGame = signal<GameSearchResult | null>(null);
  searchingGames = signal(false);

  sortOptions = [
    { value: 'votes', label: 'Most Votes' },
    { value: 'usage', label: 'Most Used' },
    { value: 'newest', label: 'Newest' },
    { value: 'name', label: 'Name (A-Z)' },
    { value: 'creator', label: 'Creator (A-Z)' },
  ];

  constructor(
    private readonly browseService: BrowseService,
    private readonly gameService: GameService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.loadProfiles();

    // Debounce text search
    this.searchControl.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => {
        this.page.set(1);
        this.loadProfiles();
      });

    // Sort change
    this.sortControl.valueChanges.subscribe(() => {
      this.page.set(1);
      this.loadProfiles();
    });

    // Game search autocomplete
    this.gameSearchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap(query => {
          if (!query || typeof query !== 'string' || query.length < 2) {
            this.gameResults.set([]);
            return of([]);
          }
          this.searchingGames.set(true);
          return this.gameService.searchGames(query).pipe(catchError(() => of([])));
        }),
      )
      .subscribe(results => {
        this.gameResults.set(results);
        this.searchingGames.set(false);
      });
  }

  loadProfiles(): void {
    this.loading.set(true);
    this.error.set(null);

    this.browseService
      .browse(
        this.searchControl.value || undefined,
        this.selectedGame()?.igdbId,
        this.sortControl.value || 'votes',
        this.page(),
        this.pageSize(),
      )
      .subscribe({
        next: response => {
          this.profiles.set(response.items);
          this.totalCount.set(response.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load profiles.');
          this.loading.set(false);
        },
      });
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadProfiles();
  }

  displayGame(game: GameSearchResult): string {
    return game?.name ?? '';
  }

  selectGame(game: GameSearchResult): void {
    this.selectedGame.set(game);
    this.page.set(1);
    this.loadProfiles();
  }

  clearGameFilter(): void {
    this.selectedGame.set(null);
    this.gameSearchControl.setValue('');
    this.page.set(1);
    this.loadProfiles();
  }

  openProfile(profile: BrowseProfileItem): void {
    this.router.navigate(['/browse', profile.id]);
  }

  vote(event: Event, profile: BrowseProfileItem, vote: number): void {
    event.stopPropagation();
    const newVote = profile.userVote === vote ? 0 : vote;

    this.browseService.vote(profile.id, newVote).subscribe({
      next: response => {
        const updated = this.profiles().map(p =>
          p.id === profile.id
            ? { ...p, voteScore: response.newScore, userVote: response.userVote === 0 ? null : response.userVote }
            : p,
        );
        this.profiles.set(updated);
      },
      error: err => {
        this.snackBar.open(err.error?.errors?.[0] ?? 'Failed to vote.', 'OK', { duration: 3000 });
      },
    });
  }

  startUsing(event: Event, profile: BrowseProfileItem): void {
    event.stopPropagation();
    this.browseService.startUsing(profile.id).subscribe({
      next: () => {
        const updated = this.profiles().map(p =>
          p.id === profile.id ? { ...p, isUsing: true, usageCount: p.usageCount + 1 } : p,
        );
        this.profiles.set(updated);
        this.snackBar.open('Profile added to your dashboard.', 'OK', { duration: 3000 });
      },
      error: err => {
        this.snackBar.open(err.error?.errors?.[0] ?? 'Failed to use profile.', 'OK', { duration: 3000 });
      },
    });
  }

  copyProfile(event: Event, profile: BrowseProfileItem): void {
    event.stopPropagation();
    this.browseService.copyProfile(profile.id).subscribe({
      next: result => {
        this.snackBar.open(`Copied as "${result.name}". Check your dashboard.`, 'OK', { duration: 4000 });
      },
      error: () => {
        this.snackBar.open('Failed to copy profile.', 'OK', { duration: 3000 });
      },
    });
  }
}
