import { Injectable, signal, effect } from '@angular/core';

export type ThemeMode = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'theme_mode';

  private readonly _mode = signal<ThemeMode>(this.loadPreference());
  readonly mode = this._mode.asReadonly();
  readonly isDark = () => this._mode() === 'dark';

  constructor() {
    effect(() => {
      const mode = this._mode();
      document.body.style.colorScheme = mode;
      localStorage.setItem(this.storageKey, mode);
    });
  }

  toggle(): void {
    this._mode.set(this._mode() === 'dark' ? 'light' : 'dark');
  }

  private loadPreference(): ThemeMode {
    const stored = localStorage.getItem(this.storageKey);
    if (stored === 'light' || stored === 'dark') return stored;
    return 'dark';
  }
}
