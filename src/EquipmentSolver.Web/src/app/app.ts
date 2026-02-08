import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from './services/auth.service';
import { ThemeService } from './services/theme.service';
import { NotificationService } from './services/notification.service';
import { ConfirmDialogComponent, ConfirmDialogData } from './components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatMenuModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  deleting = signal(false);

  constructor(
    readonly authService: AuthService,
    readonly themeService: ThemeService,
    private readonly notification: NotificationService,
    private readonly dialog: MatDialog,
  ) {}

  logout(): void {
    this.authService.logout();
  }

  deleteAccount(): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Delete Account',
        message: 'Are you sure you want to delete your account? This action cannot be undone. All your profiles and data will be permanently deleted.',
        confirmText: 'Delete',
        warn: true,
      } satisfies ConfirmDialogData,
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.deleting.set(true);
      this.authService.deleteAccount().subscribe({
        next: () => {
          this.deleting.set(false);
          this.authService.logout();
          this.notification.success('Account deleted successfully.');
        },
        error: () => {
          this.deleting.set(false);
          this.notification.error('Failed to delete account. Please try again.');
        },
      });
    });
  }
}
