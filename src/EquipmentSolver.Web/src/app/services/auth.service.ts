import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, RefreshRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly tokenKey = 'access_token';
  private readonly refreshKey = 'refresh_token';
  private readonly expiresKey = 'expires_at';
  private readonly usernameKey = 'username';

  private readonly _isAuthenticated = signal(this.hasValidToken());
  readonly isAuthenticated = this._isAuthenticated.asReadonly();

  private readonly _username = signal(localStorage.getItem(this.usernameKey) ?? '');
  readonly username = this._username.asReadonly();

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request).pipe(
      tap(response => this.storeTokens(response, request.username)),
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => this.storeTokens(response, request.username)),
    );
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }
    const request: RefreshRequest = { refreshToken };
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, request).pipe(
      tap(response => this.storeTokens(response, this._username())),
      catchError(err => {
        this.logout();
        return throwError(() => err);
      }),
    );
  }

  deleteAccount(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/account`).pipe(
      tap(() => this.clearTokens()),
    );
  }

  logout(): void {
    this.clearTokens();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshKey);
  }

  private hasValidToken(): boolean {
    const token = localStorage.getItem(this.tokenKey);
    const expiresAt = localStorage.getItem(this.expiresKey);
    if (!token || !expiresAt) return false;
    return new Date(expiresAt) > new Date();
  }

  private storeTokens(response: AuthResponse, username: string): void {
    localStorage.setItem(this.tokenKey, response.accessToken);
    localStorage.setItem(this.refreshKey, response.refreshToken);
    localStorage.setItem(this.expiresKey, response.expiresAt);
    localStorage.setItem(this.usernameKey, username);
    this._isAuthenticated.set(true);
    this._username.set(username);
  }

  private clearTokens(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshKey);
    localStorage.removeItem(this.expiresKey);
    localStorage.removeItem(this.usernameKey);
    this._isAuthenticated.set(false);
    this._username.set('');
  }
}
