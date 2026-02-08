import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  BrowseProfilesResponse,
  PublicProfileDetailResponse,
  VoteResponse,
  CopyResponse,
} from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class BrowseService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  // --- Visibility ---

  setVisibility(profileId: number, isPublic: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/profiles/${profileId}/visibility`, { isPublic });
  }

  // --- Browse ---

  browse(
    search?: string,
    gameId?: number,
    sort: string = 'votes',
    page: number = 1,
    pageSize: number = 20,
  ): Observable<BrowseProfilesResponse> {
    let params = new HttpParams()
      .set('sort', sort)
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (gameId) params = params.set('gameId', gameId.toString());

    return this.http.get<BrowseProfilesResponse>(`${this.apiUrl}/browse`, { params });
  }

  // --- Public Profile Detail ---

  getPublicProfile(id: number): Observable<PublicProfileDetailResponse> {
    return this.http.get<PublicProfileDetailResponse>(`${this.apiUrl}/browse/${id}`);
  }

  // --- Vote ---

  vote(profileId: number, vote: number): Observable<VoteResponse> {
    return this.http.post<VoteResponse>(`${this.apiUrl}/browse/${profileId}/vote`, { vote });
  }

  // --- Copy ---

  copyProfile(profileId: number): Observable<CopyResponse> {
    return this.http.post<CopyResponse>(`${this.apiUrl}/browse/${profileId}/copy`, {});
  }

  // --- Use ---

  startUsing(profileId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/browse/${profileId}/use`, {});
  }

  stopUsing(profileId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/browse/${profileId}/use`);
  }
}
