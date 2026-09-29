import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../core/api.service';
import { AppNotification } from '../core/models';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h1>Notifications 🔔</h1>
    <p class="muted">Invitations, confirmations, refus et rappels envoyés par le système.</p>

    <div class="card">
      @if (loading) {
        <div class="loading">Chargement…</div>
      } @else if (notifications.length === 0) {
        <div class="empty">Aucune notification.</div>
      } @else {
        <table>
          <thead><tr><th>Type</th><th>Objet</th><th>Détail</th><th>Envoi</th><th>Date</th><th></th></tr></thead>
          <tbody>
            @for (item of notifications; track item.id) {
              <tr [style.opacity]="item.isRead ? .6 : 1">
                <td><span class="badge badge-completed">{{ item.type }}</span></td>
                <td><strong>{{ item.subject }}</strong></td>
                <td class="muted">{{ item.message }}</td>
                <td>
                  <span class="badge" [class.badge-approved]="item.isSent" [class.badge-rejected]="!item.isSent">
                    {{ item.isSent ? 'Email envoyé' : 'Échec' }}
                  </span>
                </td>
                <td class="muted">{{ item.createdAt | date:'dd/MM HH:mm' }}</td>
                <td class="text-right">
                  @if (!item.isRead) {
                    <button class="btn-secondary btn-sm" (click)="markRead(item)">Marquer comme lu</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class NotificationsComponent implements OnInit {
  notifications: AppNotification[] = [];
  loading = true;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.getNotifications().subscribe({
      next: (notifications) => {
        this.notifications = notifications;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  markRead(item: AppNotification): void {
    this.api.markNotificationRead(item.id).subscribe(() => this.load());
  }
}
