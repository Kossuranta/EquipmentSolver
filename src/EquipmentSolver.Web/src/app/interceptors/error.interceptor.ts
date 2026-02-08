import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

/**
 * Global HTTP error interceptor. Shows snackbar notifications for server errors
 * and rate limiting. Auth errors (401) are handled by the auth interceptor.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notification = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Don't show snackbar for auth errors — the auth interceptor handles 401
      if (error.status === 401 || error.status === 403) {
        return throwError(() => error);
      }

      if (error.status === 429) {
        notification.error('Too many requests. Please slow down and try again.');
        return throwError(() => error);
      }

      if (error.status === 0) {
        notification.error('Unable to connect to the server. Check your connection.');
        return throwError(() => error);
      }

      if (error.status >= 500) {
        const message = extractErrorMessage(error) ?? 'A server error occurred. Please try again later.';
        notification.error(message);
        return throwError(() => error);
      }

      // For 4xx errors (except 401/403/429), don't show global notification.
      // Let individual components handle these with their own error messages.
      return throwError(() => error);
    }),
  );
};

function extractErrorMessage(error: HttpErrorResponse): string | null {
  if (error.error?.errors?.length > 0) {
    return error.error.errors[0];
  }
  if (typeof error.error?.message === 'string') {
    return error.error.message;
  }
  return null;
}
