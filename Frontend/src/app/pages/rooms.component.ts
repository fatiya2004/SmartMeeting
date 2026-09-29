import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { Room } from '../core/models';

@Component({
  selector: 'app-rooms',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h1>Salles de réunion</h1>
    <p class="muted">Salles disponibles au sein de l'établissement.</p>

    @if (message) { <div class="alert alert-success">{{ message }}</div> }
    @if (error) { <div class="alert alert-error">{{ error }}</div> }

    @if (auth.hasRole('Admin')) {
      <div class="card">
        <h2>{{ editingId ? 'Modifier la salle' : 'Ajouter une salle' }}</h2>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="form-row">
            <div class="field"><label for="name">Nom</label><input id="name" formControlName="name"></div>
            <div class="field"><label for="capacity">Capacité</label><input id="capacity" type="number" formControlName="capacity"></div>
            <div class="field"><label for="location">Emplacement</label><input id="location" formControlName="location"></div>
            <div class="field"><label for="equipment">Équipement</label><input id="equipment" formControlName="equipment"></div>
          </div>
          <div class="flex">
            <button type="submit" [disabled]="form.invalid">{{ editingId ? 'Enregistrer' : 'Ajouter' }}</button>
            @if (editingId) { <button type="button" class="btn-secondary" (click)="cancelEdit()">Annuler</button> }
          </div>
        </form>
      </div>
    }

    <div class="card">
      @if (loading) {
        <div class="loading">Chargement…</div>
      } @else {
        <table>
          <thead>
            <tr><th>Salle</th><th>Capacité</th><th>Emplacement</th><th>Équipement</th><th>État</th>
              @if (auth.hasRole('Admin')) { <th></th> }
            </tr>
          </thead>
          <tbody>
            @for (room of rooms; track room.id) {
              <tr>
                <td><strong>{{ room.name }}</strong></td>
                <td>{{ room.capacity }} places</td>
                <td>{{ room.location }}</td>
                <td class="muted">{{ room.equipment }}</td>
                <td>
                  <span class="badge" [class.badge-approved]="room.isActive" [class.badge-cancelled]="!room.isActive">
                    {{ room.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                @if (auth.hasRole('Admin')) {
                  <td class="text-right">
                    <div class="flex">
                      <button class="btn-secondary btn-sm" (click)="edit(room)">Modifier</button>
                      <button class="btn-danger btn-sm" (click)="remove(room)">Supprimer</button>
                    </div>
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class RoomsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  rooms: Room[] = [];
  loading = true;
  editingId: number | null = null;
  message = '';
  error = '';

  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    capacity: [10, [Validators.required, Validators.min(1)]],
    location: [''],
    equipment: [''],
    isActive: [true]
  });

  constructor(private api: ApiService, public auth: AuthService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.getRooms().subscribe({
      next: (rooms) => {
        this.rooms = rooms;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  submit(): void {
    const value = this.form.getRawValue();
    const request = this.editingId
      ? this.api.updateRoom(this.editingId, value)
      : this.api.createRoom(value);

    request.subscribe({
      next: () => {
        this.message = this.editingId ? 'Salle modifiée.' : 'Salle ajoutée.';
        this.cancelEdit();
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Opération impossible.')
    });
  }

  edit(room: Room): void {
    this.editingId = room.id;
    this.form.patchValue(room);
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form.reset({ name: '', capacity: 10, location: '', equipment: '', isActive: true });
  }

  remove(room: Room): void {
    if (!confirm(`Supprimer la salle « ${room.name} » ?`)) {
      return;
    }

    this.api.deleteRoom(room.id).subscribe({
      next: (response) => {
        this.message = response.message;
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Suppression impossible.')
    });
  }
}
