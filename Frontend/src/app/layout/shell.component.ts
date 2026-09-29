import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

interface NavItem { label: string; path: string; roles: string[]; }

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="layout">
      <aside class="sidebar" [class.open]="menuOpen">
        <div class="sidebar-brand">
          <div class="sidebar-logo">
            <img src="assets/images/chu-logo.png" style="width:64px;height:64px;border-radius:14px;object-fit:contain;background:white;padding:5px;box-shadow:0 4px 12px rgba(0,0,0,.2)">
            <div class="sidebar-logo-text">
              <div class="hospital">CHU Mohammed VI d'Oujda</div>
              <div class="app-name">SmartMeeting</div>
            </div>
          </div>
          <div class="sidebar-subtitle">Gestion des salles de réunions</div>
        </div>

        <nav class="sidebar-nav">
          <div class="nav-section">Navigation</div>
          @for (item of visibleItems(); track item.path) {
            <a [routerLink]="item.path" routerLinkActive="active" (click)="menuOpen = false">
              {{ item.label }}
            </a>
          }
        </nav>

        <div class="sidebar-footer">
          SmartMeeting v1.0 · CHU Oujda
        </div>
      </aside>

      <div class="main">
        <header class="navbar">
          <div class="flex">
            <button class="btn-secondary btn-sm" style="display:none" (click)="menuOpen = !menuOpen">Menu</button>
            <span style="color:var(--text-muted);font-size:13.5px;font-weight:500">
              Gestion intelligente des réunions et des ressources hospitalières
            </span>
          </div>
          <div class="flex">
            <div style="text-align:right">
              <div style="font-weight:700;font-size:14px">{{ auth.user()?.fullName }}</div>
              <div style="font-size:12px;color:var(--text-muted)">{{ roleLabel() }} · {{ auth.user()?.service }}</div>
            </div>
            <div class="avatar">{{ initials() }}</div>
            <button class="btn-secondary btn-sm" (click)="auth.logout()">Déconnexion</button>
          </div>
        </header>

        <main class="content">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `
})
export class ShellComponent {
  menuOpen = false;

  private readonly items: NavItem[] = [
    { label: 'Tableau de bord', path: '/dashboard', roles: ['Admin', 'Secretaire', 'Participant'] },
    { label: 'Mes réunions', path: '/my-meetings', roles: ['Admin', 'Secretaire', 'Participant'] },
    { label: 'Nouvelle réunion', path: '/meetings/new', roles: ['Admin', 'Secretaire', 'Participant'] },
    { label: 'Assistant IA', path: '/assistant', roles: ['Admin', 'Secretaire', 'Participant'] },
    { label: 'Indisponibilités', path: '/unavailability', roles: ['Admin', 'Secretaire', 'Participant'] },
    { label: 'Procès-verbaux', path: '/minutes', roles: ['Admin', 'Secretaire', 'Participant'] },
    { label: 'Notifications', path: '/notifications', roles: ['Admin', 'Secretaire', 'Participant'] },
    { label: 'Demandes', path: '/secretary', roles: ['Admin', 'Secretaire'] },
    { label: 'Salles', path: '/rooms', roles: ['Admin', 'Secretaire'] },
    { label: 'Statistiques', path: '/statistics', roles: ['Admin', 'Secretaire'] },
    { label: 'Utilisateurs', path: '/users', roles: ['Admin'] }
  ];

  constructor(public auth: AuthService) {}

  visibleItems(): NavItem[] {
    const role = this.auth.user()?.role ?? 'Participant';
    return this.items.filter((item) => item.roles.includes(role));
  }

  initials(): string {
    const user = this.auth.user();
    return user ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase() : '?';
  }

  roleLabel(): string {
    switch (this.auth.user()?.role) {
      case 'Admin': return 'Administrateur';
      case 'Secretaire': return 'Secrétaire';
      default: return 'Personnel médical';
    }
  }
}

