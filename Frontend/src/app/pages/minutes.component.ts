import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { Meeting, Minutes } from '../core/models';
import { frDate } from '../core/ui';

@Component({
  selector: 'app-minutes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1>Procès-verbaux 📝</h1>
    <p class="muted">Saisissez vos notes brutes : l'IA les transforme en compte rendu structuré (décisions et tâches).</p>

    @if (error) { <div class="alert alert-error">{{ error }}</div> }

    <div class="card">
      <div class="field">
        <label for="meeting">Réunion</label>
        <select id="meeting" [(ngModel)]="meetingId">
          <option [ngValue]="null">— Choisir une réunion —</option>
          @for (meeting of meetings; track meeting.id) {
            <option [ngValue]="meeting.id">{{ meeting.title }} — {{ frDate(meeting.date) }}</option>
          }
        </select>
      </div>

      <div class="field">
        <label for="notes">Notes de réunion</label>
        <textarea id="notes" [(ngModel)]="notes" style="min-height:140px"
          placeholder="Ahmed doit préparer le rapport avant vendredi. Sara envoie les documents demain."></textarea>
      </div>

      <button (click)="generate()" [disabled]="!meetingId || !notes.trim() || loading">
        {{ loading ? 'Génération…' : '✨ Générer le compte rendu' }}
      </button>
    </div>

    @if (generated) {
      <div class="card">
        <h2>{{ generated.meetingTitle }}</h2>
        <pre style="white-space:pre-wrap;font-family:inherit">{{ generated.generatedContent }}</pre>
      </div>
    }

    <div class="card">
      <h2>Comptes rendus existants</h2>
      @if (history.length === 0) {
        <div class="empty">Aucun compte rendu enregistré.</div>
      } @else {
        @for (item of history; track item.id) {
          <div style="border-bottom:1px solid var(--border);padding:12px 0">
            <strong>{{ item.meetingTitle }}</strong>
            <div class="muted" style="font-size:12px">{{ item.createdAt | date:'dd/MM/yyyy HH:mm' }}</div>
            <pre style="white-space:pre-wrap;font-family:inherit;margin-top:8px">{{ item.generatedContent }}</pre>
          </div>
        }
      }
    </div>
  `
})
export class MinutesComponent implements OnInit {
  meetings: Meeting[] = [];
  history: Minutes[] = [];
  generated?: Minutes;
  meetingId: number | null = null;
  notes = '';
  loading = false;
  error = '';

  readonly frDate = frDate;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getMyMeetings().subscribe((meetings) => (this.meetings = meetings));
    this.loadHistory();
  }

  loadHistory(): void {
    this.api.getMinutes().subscribe((history) => (this.history = history));
  }

  generate(): void {
    if (!this.meetingId) {
      return;
    }

    this.loading = true;
    this.error = '';

    this.api.generateMinutes(this.meetingId, this.notes).subscribe({
      next: (minutes) => {
        this.generated = minutes;
        this.loading = false;
        this.loadHistory();
      },
      error: (err) => {
        this.error = err.error?.message ?? 'Génération impossible.';
        this.loading = false;
      }
    });
  }
}
