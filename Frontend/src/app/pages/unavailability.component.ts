import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { Unavailability } from '../core/models';
import { frDate, shortTime } from '../core/ui';

@Component({
  selector: 'app-unavailability',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h1>Mes indisponibilités</h1>
    <p class="muted">Déclarez vos gardes, blocs, consultations et congés : aucune réunion ne pourra être planifiée sur ces créneaux.</p>

    @if (message) { <div class="alert alert-success">{{ message }}</div> }
    @if (error) { <div class="alert alert-error">{{ error }}</div> }

    <div class="card">
      <h2>Déclarer une indisponibilité</h2>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="form-row">
          <div class="field">
            <label for="type">Type</label>
            <select id="type" formControlName="type">
              <option value="Garde">Garde</option>
              <option value="BlocOperatoire">Bloc opératoire</option>
              <option value="Consultation">Consultation</option>
              <option value="Conge">Congé</option>
              <option value="Autre">Autre</option>
            </select>
          </div>
          <div class="field"><label for="date">Date</label><input id="date" type="date" formControlName="date"></div>
          <div class="field"><label for="startTime">Début</label><input id="startTime" type="time" formControlName="startTime"></div>
          <div class="field"><label for="endTime">Fin</label><input id="endTime" type="time" formControlName="endTime"></div>
        </div>
        <div class="field"><label for="reason">Motif (facultatif)</label><input id="reason" formControlName="reason"></div>
        <button type="submit" [disabled]="form.invalid">Enregistrer</button>
      </form>
    </div>

    <div class="card">
      <h2>Mes déclarations</h2>
      @if (loading) {
        <div class="loading">Chargement…</div>
      } @else if (items.length === 0) {
        <div class="empty">Aucune indisponibilité déclarée.</div>
      } @else {
        <table>
          <thead><tr><th>Type</th><th>Date</th><th>Horaire</th><th>Motif</th><th></th></tr></thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td><span class="badge badge-pending">{{ label(item.type) }}</span></td>
                <td>{{ frDate(item.date) }}</td>
                <td>{{ shortTime(item.startTime) }} – {{ shortTime(item.endTime) }}</td>
                <td class="muted">{{ item.reason || '—' }}</td>
                <td class="text-right"><button class="btn-danger btn-sm" (click)="remove(item)">Supprimer</button></td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class UnavailabilityComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  items: Unavailability[] = [];
  loading = true;
  message = '';
  error = '';

  readonly shortTime = shortTime;
  readonly frDate = frDate;

  form = this.fb.nonNullable.group({
    type: ['Garde', Validators.required],
    date: ['', Validators.required],
    startTime: ['08:00', Validators.required],
    endTime: ['18:00', Validators.required],
    reason: ['']
  });

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.getMyUnavailabilities().subscribe({
      next: (items) => {
        this.items = items;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  label(type: string): string {
    const labels: Record<string, string> = {
      Garde: 'Garde',
      BlocOperatoire: 'Bloc opératoire',
      Consultation: 'Consultation',
      Conge: 'Congé',
      Autre: 'Autre'
    };
    return labels[type] ?? type;
  }

  submit(): void {
    this.error = '';
    this.api.createUnavailability(this.form.getRawValue() as Partial<Unavailability>).subscribe({
      next: () => {
        this.message = 'Indisponibilité enregistrée.';
        this.form.patchValue({ reason: '' });
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Enregistrement impossible.')
    });
  }

  remove(item: Unavailability): void {
    this.api.deleteUnavailability(item.id).subscribe({
      next: () => {
        this.message = 'Indisponibilité supprimée.';
        this.load();
      },
      error: (err) => (this.error = err.error?.message ?? 'Suppression impossible.')
    });
  }
}
