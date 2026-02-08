import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { GameSearchResult } from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly apiUrl = `${environment.apiUrl}/games`;

  constructor(private readonly http: HttpClient) {}

  /// Search IGDB for games. Results are cached server-side for 24h.
  searchGames(query: string, limit: number = 20): Observable<GameSearchResult[]> {
    if (!query || query.trim().length < 2) {
      return of([]);
    }
    return this.http.get<GameSearchResult[]>(`${this.apiUrl}/search`, {
      params: { q: query.trim(), limit: limit.toString() },
    });
  }
}
