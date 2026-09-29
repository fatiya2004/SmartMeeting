import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../core/api.service';
import { AlternativeSlot, AvailabilityResult, Room, User } from '../core/models';
import { frDate, shortTime } from '../core/ui';

@Component({
  selector: 'app-meeting-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h1>Nouvelle réunion</h1>
    <p class="muted">Formulaire classique. Pour une demande en langage naturel, utilisez l'assistant IA.</p>

    @if (error) { <div class="alert alert-error">{{ error }}</div> }
    @if (success) { <div class="alert alert-success">{{ success }}</div> }

    <div class="card">
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="field">
          <label for="title">Titre de la réunion</label>
          <input id="title" formControlName="title" placeholder="Staff de cardiologie">
        </div>

        <div class="field">
          <label for="description">Description</label>
          <textarea id="description" formControlName="description" placeholder="Ordre du jour…"></textarea>
        </div>

        <div class="form-row">
          <div class="field">
            <label for="date">Date</label>
            <input id="date" type="date" formControlName="date">
          </div>
          <div class="field">
            <label for="startTime">Heure de début</label>
            <input id="startTime" type="time" formControlName="startTime">
          </div>
          <div class="field">
            <label for="endTime">Heure de fin</label>
            <input id="endTime" type="time" formControlName="endTime">
          </div>
          <div class="field">
            <label for="priority">Priorité</label>
            <select id="priority" formControlName="priority">
              <option value="Normal">Normale</option>
              <option value="High">Haute</option>
              <option value="Urgent">Urgente</option>
            </select>
          </div>
        </div>

        <div class="field">
          <label for="roomId">Salle</label>
          <select id="roomId" formControlName="roomId">
            <option [ngValue]="null">— Choisir une salle —</option>
            @for (room of rooms; track room.id) {
              <option [ngValue]="room.id">{{ room.name }} ({{ room.capacity }} places)</option>
            }
          </select>
        </div>

        <div class="field">
          <label>Participants</label>
          <div class="grid grid-3">
            @for (user of users; track user.id) {
              <label class="flex" style="font-weight:400">
                <input type="checkbox" style="width:auto" [checked]="selected.has(user.id)"
                       (change)="toggle(user.id)">
                <span>{{ user.fullName }} <span class="muted">· {{ user.service }}</span></span>
              </label>
            }
          </div>
        </div>

        <div class="flex mt-3">
          <button type="button" class="btn-secondary" (click)="check()" [disabled]="form.invalid || checking">
            {{ checking ? 'Vérification…' : 'Vérifier les disponibilités' }}
          </button>
          <button type="submit" [disabled]="form.invalid || saving">
            {{ saving ? 'Envoi…' : 'Envoyer la demande' }}
          </button>
        </div>
      </form>
    </div>

    @if (availability) {
      <div class="card">
        <h2>Résultat de la vérification</h2>

        @if (availability.isAvailable) {
          <div class="alert alert-success">✅ Créneau disponible : aucun conflit détecté.</div>
        } @else {
          <div class="alert alert-warn">⚠️ Conflit détecté</div>
          <ul>
            @for (conflict of availability.conflicts; track conflict.message) {
              <li>{{ conflict.message }}</li>
            }
          </ul>

          @if (availability.alternatives.length > 0) {
            <h3 class="mt-3">Créneaux alternatifs</h3>
            <div class="grid grid-3">
              @for (slot of availability.alternatives; track slot.date + slot.startTime + slot.roomId) {
                <div class="card" style="margin:0">
                  <strong>{{ frDate(slot.date) }} · {{ shortTime(slot.startTime) }} → {{ shortTime(slot.endTime) }}</strong>
                  <div class="muted">{{ slot.roomName }}</div>
                  <p class="muted" style="font-size:12.5px">{{ slot.justification }}</p>
                  <button class="btn-sm" (click)="applySlot(slot)">Choisir ce créneau</button>
                </div>
              }
            </div>
          }
        }
      </div>
    }
  `
})
export class MeetingFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  rooms: Room[] = [];
  users: User[] = [];
  selected = new Set<number>();
  availability?: AvailabilityResult;
  checking = false;
  saving = false;
  error = '';
  success = '';

  readonly shortTime = shortTime;
  readonly frDate = frDate;

  form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    description: [''],
    date: ['', Validators.required],
    startTime: ['09:00', Validators.required],
    endTime: ['10:00', Validators.required],
    priority: ['Normal', Validators.required],
    roomId: this.fb.control<number | null>(null)
  });

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.api.getRooms(true).subscribe((rooms) => (this.rooms = rooms));
    this.api.getUsers().subscribe((users) => (this.users = users.filter((u) => u.isActive)));
  }

  toggle(userId: number): void {
    if (this.selected.has(userId)) {
      this.selected.delete(userId);
    } else {
      this.selected.add(userId);
    }
  }

  private payload() {
    const value = this.form.getRawValue();
    return {
      title: value.title,
      description: value.description,
      date: value.date,
      startTime: value.startTime,
      endTime: value.endTime,
      priority: value.priority as 'Normal' | 'High' | 'Urgent',
      roomId: value.roomId,
      participantIds: Array.from(this.selected)
    };
  }

  check(): void {
    this.checking = true;
    this.error = '';
    const data = this.payload();

    this.api.checkAvailability({
      date: data.date,
      startTime: data.startTime,
      endTime: data.endTime,
      roomId: data.roomId,
      participantIds: data.participantIds
    }, data.priority).subscribe({
      next: (result) => {
        this.availability = result;
        this.checking = false;
      },
      error: (err) => {
        this.error = err.error?.message ?? 'Vérification impossible.';
        this.checking = false;
      }
    });
  }

  applySlot(slot: AlternativeSlot): void {
    this.form.patchValue({
      date: slot.date,
      startTime: shortTime(slot.startTime),
      endTime: shortTime(slot.endTime),
      roomId: slot.roomId
    });
    this.availability = undefined;
  }

  submit(): void {
    this.saving = true;
    this.error = '';

    this.api.createMeeting(this.payload()).subscribe({
      next: () => {
        this.success = 'Demande envoyée au secrétariat. Les invitations ont été transmises par email.';
        this.saving = false;
        setTimeout(() => this.router.navigate(['/my-meetings']), 1200);
      },
      error: (err) => {
        this.error = err.error?.message ?? 'Création impossible.';
        this.saving = false;
      }
    });
  }
}
