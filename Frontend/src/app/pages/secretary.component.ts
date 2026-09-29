import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { Meeting } from '../core/models';
import { frDate, priorityClasses, priorityLabels, shortTime, statusClasses, statusLabels } from '../core/ui';

@Component({
  selector: 'app-secretary',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1>Tableau de bord du secrétariat</h1>
    <p class="muted">Validez, refusez ou arbitrez les demandes de réunion.</p>

    @if (message) { <div class="alert alert-success">{{ message }}</div> }
    @if (error) { <div class="alert alert-error">{{ error }}</div> }

    <div class="grid grid-4 mb-3">
      <div class="stat"><div class="label">En attente</div><div class="value">{{ count('Pending') }}</div></div>
      <div class="stat"><div class="label">Approuvées</div><div class="value">{{ count('Approved') }}</div></div>
      <div class="stat"><div class="label">Refusées</div><div class="value">{{ count('Rejected') }}</div></div>
      <div class="stat"><div class="label">Urgentes</div><div class="value">{{ urgentCount() }}</div></div>
    </div>

    <div class="card">
      <div class="card-header">
        <h2>Demandes</h2>
        <select [(ngModel)]="filter" (change)="load()" style="width:200px">
          <option value="Pending">En attente</option>
          <option value="Approved">Approuvées</option>
          <option value="Rejected">Refusées</option>
          <option value="">Toutes</option>
        </select>
      </div>

      @if (loading) {
        <div class="loading">Chargement…</div>
      } @else if (meetings.length === 0) {
        <div class="empty">Aucune demande dans cette catégorie.</div>
      } @else {
        <table>
          <thead>
            <tr><th>Réunion</th><th>Demandeur</th><th>Date</th><th>Horaire</th><th>Salle</th><th>Priorité</th><th>Statut</th><th></th></tr>
          </thead>
          <tbody>
            @for (meeting of meetings; track meeting.id) {
              <tr>
                <td>
                  <strong>{{ meeting.title }}</strong>
                  <div class="muted" style="font-size:12px">{{ meeting.participants.length }} participant(s)</div>
                </td>
                <td>{{ meeting.createdByName }}</td>
                <td>{{ frDate(meeting.date) }}</td>
                <td>{{ shortTime(meeting.startTime) }} – {{ shortTime(meeting.endTime) }}</td>
                <td>{{ meeting.roomName ?? '—' }}</td>
                <td><span [class]="priorityClasses[meeting.priority]">{{ priorityLabels[meeting.priority] }}</span></td>
                <td><span [class]="statusClasses[meeting.status]">{{ statusLabels[meeting.status] }}</span></td>
                <td class="text-right">
                  @if (meeting.status === 'Pending') {
                    <div class="flex">
                      <button class="btn-success btn-sm" (click)="approve(meeting)">Accepter</button>
                      <button class="btn-danger btn-sm" (click)="reject(meeting)">Refuser</button>
                    </div>
                  } @else {
                    <span class="muted">{{ meeting.decisionReason ?? '—' }}</span>
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
export class SecretaryComponent implements OnInit {
  meetings: Meeting[] = [];
  allMeetings: Meeting[] = [];
  filter = 'Pending';
  loading = true;
  message = '';
  error = '';

  readonly statusLabels = statusLabels;
  readonly statusClasses = statusClasses;
  readonly priorityLabels = priorityLabels;
  readonly priorityClasses = priorityClasses;
  readonly shortTime = shortTime;
  readonly frDate = frDate;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;

    this.api.getAllMeetings().subscribe({
      next: (meetings) => {
        this.allMeetings = meetings;
        this.meetings = this.filter ? meetings.filter((m) => m.status === this.filter) : meetings;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  count(status: string): number {
    return this.allMeetings.filter((m) => m.status === status).length;
  }

  urgentCount(): number {
    return this.allMeetings.filter((m) => m.priority === 'Urgent' && m.status !== 'Cancelled').length;
  }

  approve(meeting: Meeting): void {
    this.error = '';
    this.api.approveMeeting(meeting.id).subscribe({
      next: () => {
        this.message = `Réunion « ${meeting.title} » validée. Les emails ont été envoyés.`;
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Validation impossible.')
    });
  }

  reject(meeting: Meeting): void {
    const reason = prompt('Motif du refus :', 'Créneau non disponible') ?? '';
    this.api.rejectMeeting(meeting.id, reason).subscribe({
      next: () => {
        this.message = `Réunion « ${meeting.title} » refusée.`;
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Refus impossible.')
    });
  }
}
