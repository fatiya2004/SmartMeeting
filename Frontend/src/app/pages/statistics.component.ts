import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../core/api.service';
import { GlobalStats, LabelValue } from '../core/models';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h1>Statistiques 📊</h1>
    <p class="muted">Indicateurs d'activité de la plateforme.</p>

    @if (loading) {
      <div class="loading">Chargement…</div>
    } @else if (stats) {
      <div class="grid grid-4 mb-3">
        <div class="stat"><div class="label">Total réunions</div><div class="value">{{ stats.totalMeetings }}</div></div>
        <div class="stat"><div class="label">Approuvées</div><div class="value">{{ stats.approved }}</div></div>
        <div class="stat"><div class="label">Refusées</div><div class="value">{{ stats.rejected }}</div></div>
        <div class="stat"><div class="label">Temps gagné estimé</div><div class="value">{{ stats.estimatedHoursSaved }} h</div></div>
      </div>

      <div class="grid grid-2">
        <div class="card">
          <h2>Réunions par jour (7 derniers jours)</h2>
          <div class="bars">
            @for (item of stats.meetingsPerDay; track item.label) {
              <div class="bar-wrap">
                <div class="bar" [style.height.%]="height(item, stats.meetingsPerDay)"></div>
                <div class="bar-label">{{ item.label }}</div>
                <div class="bar-label"><strong>{{ item.value }}</strong></div>
              </div>
            }
          </div>
        </div>

        <div class="card">
          <h2>Réunions par priorité</h2>
          <div class="bars">
            @for (item of stats.meetingsPerPriority; track item.label) {
              <div class="bar-wrap">
                <div class="bar" [style.height.%]="height(item, stats.meetingsPerPriority)"></div>
                <div class="bar-label">{{ item.label }}</div>
                <div class="bar-label"><strong>{{ item.value }}</strong></div>
              </div>
            }
          </div>
        </div>
      </div>

      <div class="card">
        <h2>Occupation des salles (heures réservées)</h2>
        <table>
          <thead><tr><th>Salle</th><th>Heures</th><th>Occupation relative</th></tr></thead>
          <tbody>
            @for (item of stats.roomOccupancy; track item.label) {
              <tr>
                <td>{{ item.label }}</td>
                <td>{{ item.value }} h</td>
                <td>
                  <div style="background:var(--primary-light);border-radius:6px;height:10px;width:100%">
                    <div [style.width.%]="height(item, stats.roomOccupancy)"
                         style="background:var(--primary);height:10px;border-radius:6px"></div>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `
})
export class StatisticsComponent implements OnInit {
  stats?: GlobalStats;
  loading = true;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getGlobalStats().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  height(item: LabelValue, all: LabelValue[]): number {
    const max = Math.max(...all.map((i) => i.value), 1);
    return Math.round((item.value / max) * 100);
  }
}
