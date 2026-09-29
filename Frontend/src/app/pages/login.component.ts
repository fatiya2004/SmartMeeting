import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <section class="auth-visual">
        <div class="auth-visual-logo">
          <img src="assets/images/chu-logo.png" style="width:90px;height:90px;border-radius:20px;object-fit:contain;background:white;padding:6px;box-shadow:0 6px 18px rgba(74,159,212,.3)">
          <div class="auth-visual-logo-text">
            <div class="chu">CHU Mohammed VI d'Oujda</div>
            <div class="name">SmartMeeting</div>
            <div class="sub">Gestion des salles de réunions</div>
          </div>
        </div>

        <h1>Bienvenue sur la plateforme de gestion des réunions</h1>
        <p>Planifiez vos réunions, gérez les salles et coordonnez vos équipes médicales en toute simplicité.</p>

        <ul class="auth-features">
          <li><span class="check">✓</span>Détection automatique des conflits</li>
          <li><span class="check">✓</span>Prise en compte des gardes et du bloc</li>
          <li><span class="check">✓</span>Assistant IA en langage naturel</li>
          <li><span class="check">✓</span>Validation par le secrétariat médical</li>
          <li><span class="check">✓</span>Notifications email automatiques</li>
        </ul>
      </section>

      <section class="auth-form-side">
        <div class="auth-box">
          <div class="auth-box-header">
            <div class="title">Connexion</div>
            <div class="subtitle">Accédez à votre espace SmartMeeting</div>
          </div>

          @if (error) { <div class="alert alert-error">{{ error }}</div> }

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="field">
              <label for="email">Email professionnel</label>
              <input id="email" type="email" formControlName="email" placeholder="prenom.nom@chu-demo.ma">
            </div>
            <div class="field">
              <label for="password">Mot de passe</label>
              <input id="password" type="password" formControlName="password" placeholder="••••••••">
            </div>
            <button type="submit" style="width:100%;padding:13px" [disabled]="form.invalid || loading">
              {{ loading ? 'Connexion…' : 'Se connecter' }}
            </button>
          </form>

          <p class="mt-3 muted" style="font-size:13px;text-align:center">
            Pas encore de compte ? <a routerLink="/register">Créer un compte</a>
          </p>

          <div class="alert alert-info mt-3" style="font-size:12.5px">
            <strong>Comptes de démonstration</strong><br>
            admin&#64;chu-demo.ma · secretaire&#64;chu-demo.ma<br>
            ahmed.benali&#64;chu-demo.ma · Mot de passe : Password123!
          </div>
        </div>
      </section>
    </div>
  `
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  loading = false;
  error = '';

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  constructor(private auth: AuthService, private router: Router) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => { this.error = err.error?.message ?? 'Connexion impossible.'; this.loading = false; }
    });
  }
}

