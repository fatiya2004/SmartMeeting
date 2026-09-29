import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { Meeting } from '../core/models';
import { frDate, priorityClasses, priorityLabels, shortTime, statusClasses, statusLabels } from '../core/ui';

@Component({
  selector: 'app-my-meetings',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h1>Mes réunions</h1>
    <p class="muted">Réunions que vous avez créées ou auxquelles vous êtes invité(e).</p>

    @if (message) { <div class="alert alert-success">{{ message }}</div> }
    @if (error) { <div class="alert alert-error">{{ error }}</div> }

    <div class="card">
      @if (loading) {
        <div class="loading">Chargement…</div>
      } @else if (meetings.length === 0) {
        <div class="empty">Aucune réunion pour le moment.</div>
      } @else {
        <table>
          <thead>
            <tr><th>Titre</th><th>Date</th><th>Horaire</th><th>Salle</th><th>Priorité</th><th>Statut</th><th>Ma réponse</th><th></th></tr>
          </thead>
          <tbody>
            @for (meeting of meetings; track meeting.id) {
              <tr>
                <td><strong>{{ meeting.title }}</strong><div class="muted" style="font-size:12px">{{ meeting.createdByName }}</div></td>
                <td>{{ frDate(meeting.date) }}</td>
                <td>{{ shortTime(meeting.startTime) }} – {{ shortTime(meeting.endTime) }}</td>
                <td>{{ meeting.roomName ?? '—' }}</td>
                <td><span [class]="priorityClasses[meeting.priority]">{{ priorityLabels[meeting.priority] }}</span></td>
                <td><span [class]="statusClasses[meeting.status]">{{ statusLabels[meeting.status] }}</span></td>
                <td>{{ myResponse(meeting) }}</td>
                <td class="text-right">
                  <div class="flex">
                    @if (canRespond(meeting)) {
                      <button class="btn-success btn-sm" (click)="respond(meeting, true)">Accepter</button>
                      <button class="btn-secondary btn-sm" (click)="respond(meeting, false)">Refuser</button>
                    }
                    @if (meeting.createdById === auth.user()?.id && meeting.status !== 'Cancelled') {
                      <button class="btn-danger btn-sm" (click)="cancel(meeting)">Annuler</button>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class MyMeetingsComponent implements OnInit {
  meetings: Meeting[] = [];
  loading = true;
  message = '';
  error = '';

  readonly statusLabels = statusLabels;
  readonly statusClasses = statusClasses;
  readonly priorityLabels = priorityLabels;
  readonly priorityClasses = priorityClasses;
  readonly shortTime = shortTime;
  readonly frDate = frDate;

  constructor(private api: ApiService, public auth: AuthService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.getMyMeetings().subscribe({
      next: (meetings) => {
        this.meetings = meetings;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  myResponse(meeting: Meeting): string {
    const me = meeting.participants.find((p) => p.userId === this.auth.user()?.id);
    if (!me) return '—';
    return me.status === 'Accepted' ? 'Présent(e)' : me.status === 'Declined' ? 'Absent(e)' : 'En attente';
  }

  canRespond(meeting: Meeting): boolean {
    const me = meeting.participants.find((p) => p.userId === this.auth.user()?.id);
    return !!me && me.status === 'Pending' && meeting.status !== 'Cancelled';
  }

  respond(meeting: Meeting, accept: boolean): void {
    this.api.respondInvitation(meeting.id, accept).subscribe({
      next: () => {
        this.message = accept ? 'Présence confirmée.' : 'Absence enregistrée.';
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Action impossible.')
    });
  }

  cancel(meeting: Meeting): void {
    this.api.cancelMeeting(meeting.id).subscribe({
      next: () => {
        this.message = 'Réunion annulée.';
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Annulation impossible.')
    });
  }
}
