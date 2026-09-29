import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <section class="auth-visual">
        <h1 style="color:#fff;font-size:28px">Rejoindre SmartMeeting</h1>
        <p style="opacity:.9">Créez votre compte pour demander et suivre vos réunions de service.</p>
      </section>

      <section class="auth-form-side">
        <div class="auth-box">
          <h1>Créer un compte</h1>

          @if (error) {
            <div class="alert alert-error">{{ error }}</div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="form-row">
              <div class="field">
                <label for="firstName">Prénom</label>
                <input id="firstName" formControlName="firstName">
              </div>
              <div class="field">
                <label for="lastName">Nom</label>
                <input id="lastName" formControlName="lastName">
              </div>
            </div>
            <div class="field">
              <label for="email">Email professionnel</label>
              <input id="email" type="email" formControlName="email">
            </div>
            <div class="field">
              <label for="service">Service</label>
              <input id="service" formControlName="service" placeholder="Cardiologie">
            </div>
            <div class="field">
              <label for="password">Mot de passe (8 caractères minimum)</label>
              <input id="password" type="password" formControlName="password">
            </div>
            <button type="submit" style="width:100%" [disabled]="form.invalid || loading">
              {{ loading ? 'Création…' : 'Créer mon compte' }}
            </button>
          </form>

          <p class="mt-3 muted" style="font-size:13px">
            Déjà inscrit ? <a routerLink="/login">Se connecter</a>
          </p>
        </div>
      </section>
    </div>
  `
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);

  loading = false;
  error = '';

  form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    service: [''],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  constructor(private auth: AuthService, private router: Router) {}

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    this.error = '';

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.error = err.error?.message ?? 'Inscription impossible.';
        this.loading = false;
      }
    });
  }
}
