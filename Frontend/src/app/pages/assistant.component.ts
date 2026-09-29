import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../core/api.service';
import { AiProposal, AlternativeSlot } from '../core/models';
import { frDate, priorityLabels, shortTime } from '../core/ui';

@Component({
  selector: 'app-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1>Assistant IA ✨</h1>
    <p class="muted">Décrivez votre réunion en langage naturel : l'assistant extrait les informations, puis le backend vérifie réellement les disponibilités.</p>

    @if (!aiConfigured) {
      <div class="alert alert-warn">
        Le service IA n'est pas configuré. Renseignez <code>LLM_API_KEY</code> dans le fichier <code>.env</code>, puis relancez les conteneurs.
      </div>
    }

    <div class="card">
      <label for="text">Décrivez votre réunion naturellement…</label>
      <textarea id="text" [(ngModel)]="text" style="min-height:130px"
        placeholder="Je veux organiser une réunion demain à 14h avec Ahmed et Sara pendant une heure, c'est urgent."></textarea>
      <div class="flex mt-3">
        <button (click)="analyze()" [disabled]="!text.trim() || loading">
          {{ loading ? 'Analyse en cours…' : '✨ Analyser avec l\\'IA' }}
        </button>
        <button class="btn-secondary" (click)="reset()">Effacer</button>
      </div>
    </div>

    @if (error) { <div class="alert alert-error">{{ error }}</div> }
    @if (success) { <div class="alert alert-success">{{ success }}</div> }

    @if (proposal) {
      <div class="card">
        <h2>Proposition de l'assistant</h2>
        <p>{{ proposal.explanation }}</p>

        <div class="grid grid-4 mt-3">
          <div class="stat"><div class="label">Date</div><div class="value" style="font-size:18px">{{ frDate(proposal.date) }}</div></div>
          <div class="stat"><div class="label">Horaire</div><div class="value" style="font-size:18px">{{ shortTime(proposal.startTime) }} → {{ shortTime(proposal.endTime) }}</div></div>
          <div class="stat"><div class="label">Priorité</div><div class="value" style="font-size:18px">{{ priorityLabels[proposal.priority] }}</div></div>
          <div class="stat"><div class="label">Salle proposée</div><div class="value" style="font-size:18px">{{ proposal.proposedRoomName ?? 'Aucune' }}</div></div>
        </div>

        <h3 class="mt-3">Participants identifiés</h3>
        @if (proposal.resolvedParticipants.length === 0) {
          <p class="muted">Aucun participant reconnu.</p>
        } @else {
          <ul>
            @for (user of proposal.resolvedParticipants; track user.id) {
              <li>{{ user.fullName }} <span class="muted">· {{ user.service }}</span></li>
            }
          </ul>
        }

        @if (proposal.unknownParticipants.length > 0) {
          <div class="alert alert-warn">
            Noms non reconnus : {{ proposal.unknownParticipants.join(', ') }}.
            Vérifiez l'orthographe ou ajoutez-les via le formulaire classique.
          </div>
        }

        @if (proposal.isAvailable) {
          <div class="alert alert-success mt-3">✅ Tous les participants et la salle sont disponibles.</div>
          <button (click)="confirm()" [disabled]="saving">
            {{ saving ? 'Envoi…' : 'Confirmer et envoyer la demande' }}
          </button>
        } @else {
          <div class="alert alert-warn mt-3">⚠️ Conflit détecté</div>
          <ul>
            @for (conflict of proposal.conflicts; track conflict.message) {
              <li>{{ conflict.message }}</li>
            }
          </ul>

          @if (proposal.alternatives.length > 0) {
            <h3 class="mt-3">Créneaux alternatifs proposés</h3>
            <div class="grid grid-3">
              @for (slot of proposal.alternatives; track slot.date + slot.startTime + slot.roomId) {
                <div class="card" style="margin:0">
                  <strong>{{ frDate(slot.date) }} · {{ shortTime(slot.startTime) }} → {{ shortTime(slot.endTime) }}</strong>
                  <div class="muted">{{ slot.roomName }} ✅ disponible</div>
                  <p class="muted" style="font-size:12.5px">{{ slot.justification }}</p>
                  <button class="btn-sm" (click)="confirm(slot)" [disabled]="saving">Choisir ce créneau</button>
                </div>
              }
            </div>
          }
        }
      </div>
    }
  `
})
export class AssistantComponent implements OnInit {
  text = '';
  proposal?: AiProposal;
  loading = false;
  saving = false;
  aiConfigured = true;
  error = '';
  success = '';

  readonly shortTime = shortTime;
  readonly frDate = frDate;
  readonly priorityLabels = priorityLabels;

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.api.aiStatus().subscribe({
      next: (status) => (this.aiConfigured = status.configured),
      error: () => (this.aiConfigured = false)
    });
  }

  analyze(): void {
    this.loading = true;
    this.error = '';
    this.success = '';

    this.api.analyzeText(this.text).subscribe({
      next: (proposal) => {
        this.proposal = proposal;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message ?? "L'analyse a échoué.";
        this.loading = false;
      }
    });
  }

  confirm(slot?: AlternativeSlot): void {
    if (!this.proposal) {
      return;
    }

    this.saving = true;
    this.error = '';

    const participantIds = this.proposal.resolvedParticipants.map((u) => u.id);

    this.api.createMeeting({
      title: this.proposal.parsed.title || 'Réunion',
      description: this.text,
      date: slot ? slot.date : this.proposal.date,
      startTime: shortTime(slot ? slot.startTime : this.proposal.startTime),
      endTime: shortTime(slot ? slot.endTime : this.proposal.endTime),
      priority: this.proposal.priority,
      roomId: slot ? slot.roomId : this.proposal.proposedRoomId ?? null,
      participantIds
    }).subscribe({
      next: () => {
        this.success = 'Demande envoyée au secrétariat.';
        this.saving = false;
        setTimeout(() => this.router.navigate(['/my-meetings']), 1200);
      },
      error: (err) => {
        this.error = err.error?.message ?? 'Création impossible.';
        this.saving = false;
      }
    });
  }

  reset(): void {
    this.text = '';
    this.proposal = undefined;
    this.error = '';
    this.success = '';
  }
}
