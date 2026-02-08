import { Component, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './register.page.html',
  styleUrl: './register.page.scss',
})
export class RegisterPage {
  form: FormGroup;
  loading = signal(false);
  errorMessage = signal('');

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {
    this.form = this.fb.group(
      {
        username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
        password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  get password(): string {
    return this.form.get('password')?.value ?? '';
  }

  hasUppercase(): boolean {
    return /[A-Z]/.test(this.password);
  }

  hasLowercase(): boolean {
    return /[a-z]/.test(this.password);
  }

  hasDigit(): boolean {
    return /\d/.test(this.password);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.errorMessage.set('');

    const { username, password } = this.form.value;
    this.authService.register({ username, password }).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: err => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.errors?.[0] ?? 'Registration failed. Please try again.');
      },
    });
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }
}
