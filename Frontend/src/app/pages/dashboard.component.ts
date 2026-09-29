import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { DashboardStats } from '../core/models';
import { frDate, priorityClasses, priorityLabels, shortTime, statusClasses, statusLabels } from '../core/ui';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <h1>Bonjour {{ auth.user()?.firstName }} 👋</h1>
    <p class="muted">Voici l'activité des réunions de votre service.</p>

    @if (loading) {
      <div class="loading">Chargement du tableau de bord…</div>
    } @else if (stats) {
      <div class="grid grid-4 mb-3">
        <div class="stat"><div class="label">Réunions aujourd'hui</div><div class="value">{{ stats.meetingsToday }}</div></div>
        <div class="stat"><div class="label">Cette semaine</div><div class="value">{{ stats.meetingsThisWeek }}</div></div>
        <div class="stat"><div class="label">Demandes en attente</div><div class="value">{{ stats.pendingRequests }}</div></div>
        <div class="stat"><div class="label">Réunions urgentes</div><div class="value">{{ stats.urgentMeetings }}</div></div>
        <div class="stat"><div class="label">Salles actives</div><div class="value">{{ stats.activeRooms }}</div></div>
        <div class="stat"><div class="label">Taux d'occupation</div><div class="value">{{ stats.roomOccupancyRate }}%</div></div>
      </div>

      <div class="card">
        <div class="card-header">
          <h2>Prochaines réunions</h2>
          <a class="btn btn-secondary btn-sm" routerLink="/meetings/new">Nouvelle réunion</a>
        </div>

        @if (stats.upcomingMeetings.length === 0) {
          <div class="empty">Aucune réunion planifiée pour le moment.</div>
        } @else {
          <table>
            <thead>
              <tr><th>Titre</th><th>Date</th><th>Horaire</th><th>Salle</th><th>Participants</th><th>Priorité</th><th>Statut</th></tr>
            </thead>
            <tbody>
              @for (meeting of stats.upcomingMeetings; track meeting.id) {
                <tr>
                  <td><strong>{{ meeting.title }}</strong></td>
                  <td>{{ frDate(meeting.date) }}</td>
                  <td>{{ shortTime(meeting.startTime) }} – {{ shortTime(meeting.endTime) }}</td>
                  <td>{{ meeting.roomName ?? '—' }}</td>
                  <td>{{ meeting.participants.length }}</td>
                  <td><span [class]="priorityClasses[meeting.priority]">{{ priorityLabels[meeting.priority] }}</span></td>
                  <td><span [class]="statusClasses[meeting.status]">{{ statusLabels[meeting.status] }}</span></td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    }
  `
})
export class DashboardComponent implements OnInit {
  stats?: DashboardStats;
  loading = true;

  readonly statusLabels = statusLabels;
  readonly statusClasses = statusClasses;
  readonly priorityLabels = priorityLabels;
  readonly priorityClasses = priorityClasses;
  readonly shortTime = shortTime;
  readonly frDate = frDate;

  constructor(private api: ApiService, public auth: AuthService) {}

  ngOnInit(): void {
    this.api.getDashboard().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }
}
