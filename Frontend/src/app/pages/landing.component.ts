import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="landing">
      <nav class="landing-nav">
        <div class="landing-logo">
          <img src="assets/images/chu-logo.png" style="width:110px;height:110px;border-radius:22px;object-fit:contain;background:white;padding:8px;box-shadow:0 8px 24px rgba(74,159,212,.35)">
          <div class="landing-logo-text">
            <div class="chu">CHU Mohammed VI d'Oujda</div>
            <div class="name">SmartMeeting</div>
            <div class="sub">Gestion des salles de réunions</div>
          </div>
        </div>
        <div style="display:flex;gap:12px">
          <a routerLink="/login" style="padding:10px 20px;border-radius:8px;border:1.5px solid var(--primary-mid);color:var(--primary-dark);font-weight:600;font-size:14px">Connexion</a>
          <a routerLink="/register" style="padding:10px 20px;border-radius:8px;background:linear-gradient(135deg,var(--primary),var(--primary-dark));color:#fff;font-weight:600;font-size:14px;box-shadow:0 4px 12px rgba(74,159,212,.3)">Créer un compte</a>
        </div>
      </nav>

      <section class="landing-hero">
        <div>
          <div class="landing-tag">Plateforme hospitalière intelligente</div>
          <h1>La gestion des réunions,<br><span>simplifiée et intelligente</span></h1>
          <p>SmartMeeting centralise la planification des réunions au CHU Mohammed VI d'Oujda. Détection automatique des conflits, validation par le secrétariat et assistant IA pour organiser vos réunions en langage naturel.</p>
          <div class="hero-btns">
            <button class="btn-hero-primary" routerLink="/login">Accéder à la plateforme</button>
            <button class="btn-hero-secondary" routerLink="/register">Créer un compte</button>
          </div>
        </div>

        <div class="hero-visual">
          <div class="preview-card">
            <div class="tag">Demande en cours</div>
            <div class="title">Staff de cardiologie — Dr. Benali</div>
            <div class="preview-row"><span>Date</span><strong>25/09/2026</strong></div>
            <div class="preview-row"><span>Horaire</span><strong>14:00 → 15:00</strong></div>
            <div class="preview-row"><span>Salle</span><strong>Salle B - Staff</strong></div>
            <div class="preview-row"><span>Priorité</span><span style="background:#FDECEA;color:#C0392B;padding:3px 10px;border-radius:999px;font-size:12px;font-weight:700">URGENT</span></div>
            <div class="preview-row"><span>Statut</span><span style="background:#E8F8F5;color:#1ABC9C;padding:3px 10px;border-radius:999px;font-size:12px;font-weight:700">Approuvée</span></div>
          </div>
          <div class="preview-mini">
            <div class="mini-stat"><div class="val">24</div><div class="lbl">Réunions ce mois</div></div>
            <div class="mini-stat"><div class="val">4</div><div class="lbl">Salles actives</div></div>
            <div class="mini-stat"><div class="val">0</div><div class="lbl">Conflits détectés</div></div>
            <div class="mini-stat"><div class="val">98%</div><div class="lbl">Taux de validation</div></div>
          </div>
        </div>
      </section>

      <section class="features-section">
        <h2>Tout ce dont le CHU a besoin</h2>
        <p class="sub">Une plateforme complète, conçue pour l'environnement hospitalier</p>
        <div class="features-grid">
          <div class="feat-card"><div class="feat-icon">📅</div><h3>Planification intelligente</h3><p>Détection automatique des conflits de salle, de participants et des gardes médicales.</p></div>
          <div class="feat-card"><div class="feat-icon">🤖</div><h3>Assistant IA</h3><p>Organisez vos réunions en langage naturel. L'IA extrait les informations et vérifie les disponibilités.</p></div>
          <div class="feat-card"><div class="feat-icon">📋</div><h3>Validation secrétariat</h3><p>Toutes les demandes passent par le secrétariat. La décision finale reste humaine.</p></div>
          <div class="feat-card"><div class="feat-icon">📧</div><h3>Notifications email</h3><p>Invitations, confirmations, refus et rappels automatiques envoyés par email.</p></div>
          <div class="feat-card"><div class="feat-icon">🏥</div><h3>Gestion des salles</h3><p>Réservation des salles avec vérification de capacité et du matériel disponible.</p></div>
          <div class="feat-card"><div class="feat-icon">📊</div><h3>Statistiques & rapports</h3><p>Tableau de bord avec les indicateurs clés, taux d'occupation et procès-verbaux IA.</p></div>
        </div>
      </section>

      <footer class="landing-footer">
        © 2026 CHU Mohammed VI d'Oujda — SmartMeeting · Gestion des salles de réunions
      </footer>
    </div>
  `
})
export class LandingComponent {}

