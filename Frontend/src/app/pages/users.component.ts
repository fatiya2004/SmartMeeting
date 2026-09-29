import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { User } from '../core/models';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1>Utilisateurs</h1>
    <p class="muted">Gestion des comptes et des rôles.</p>

    @if (message) { <div class="alert alert-success">{{ message }}</div> }
    @if (error) { <div class="alert alert-error">{{ error }}</div> }

    <div class="card">
      @if (loading) {
        <div class="loading">Chargement…</div>
      } @else {
        <table>
          <thead><tr><th>Nom</th><th>Email</th><th>Service</th><th>Rôle</th><th>État</th><th></th></tr></thead>
          <tbody>
            @for (user of users; track user.id) {
              <tr>
                <td><strong>{{ user.fullName }}</strong></td>
                <td class="muted">{{ user.email }}</td>
                <td>{{ user.service }}</td>
                <td>
                  <select [ngModel]="user.role" (ngModelChange)="changeRole(user, $event)" style="width:160px">
                    <option value="Admin">Administrateur</option>
                    <option value="Secretaire">Secrétaire</option>
                    <option value="Participant">Participant</option>
                  </select>
                </td>
                <td>
                  <span class="badge" [class.badge-approved]="user.isActive" [class.badge-cancelled]="!user.isActive">
                    {{ user.isActive ? 'Actif' : 'Désactivé' }}
                  </span>
                </td>
                <td class="text-right">
                  <button class="btn-secondary btn-sm" (click)="toggleActive(user)">
                    {{ user.isActive ? 'Désactiver' : 'Réactiver' }}
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  loading = true;
  message = '';
  error = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.getUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  changeRole(user: User, role: string): void {
    this.api.updateUserRole(user.id, role).subscribe({
      next: () => {
        this.message = `Rôle de ${user.fullName} mis à jour.`;
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Modification impossible.')
    });
  }

  toggleActive(user: User): void {
    this.api.setUserActive(user.id, !user.isActive).subscribe({
      next: () => {
        this.message = `Compte de ${user.fullName} mis à jour.`;
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Modification impossible.')
    });
  }
}
