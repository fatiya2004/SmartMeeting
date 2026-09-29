import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AiProposal,
  AppNotification,
  AvailabilityResult,
  DashboardStats,
  GlobalStats,
  Meeting,
  MeetingRequest,
  Minutes,
  Room,
  Unavailability,
  User
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ---------- Utilisateurs ----------
  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.base}/users`);
  }

  updateUserRole(id: number, role: string): Observable<User> {
    return this.http.put<User>(`${this.base}/users/${id}/role`, {}, { params: new HttpParams().set('role', role) });
  }

  setUserActive(id: number, isActive: boolean): Observable<User> {
    return this.http.put<User>(`${this.base}/users/${id}/active`, {}, {
      params: new HttpParams().set('isActive', isActive)
    });
  }

  // ---------- Salles ----------
  getRooms(onlyActive = false): Observable<Room[]> {
    return this.http.get<Room[]>(`${this.base}/rooms`, {
      params: new HttpParams().set('onlyActive', onlyActive)
    });
  }

  createRoom(room: Partial<Room>): Observable<Room> {
    return this.http.post<Room>(`${this.base}/rooms`, room);
  }

  updateRoom(id: number, room: Partial<Room>): Observable<Room> {
    return this.http.put<Room>(`${this.base}/rooms/${id}`, room);
  }

  deleteRoom(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.base}/rooms/${id}`);
  }

  // ---------- Réunions ----------
  getAllMeetings(status?: string): Observable<Meeting[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Meeting[]>(`${this.base}/meetings`, { params });
  }

  getMyMeetings(): Observable<Meeting[]> {
    return this.http.get<Meeting[]>(`${this.base}/meetings/mine`);
  }

  getMeeting(id: number): Observable<Meeting> {
    return this.http.get<Meeting>(`${this.base}/meetings/${id}`);
  }

  createMeeting(request: MeetingRequest): Observable<Meeting> {
    return this.http.post<Meeting>(`${this.base}/meetings`, request);
  }

  updateMeeting(id: number, request: MeetingRequest): Observable<Meeting> {
    return this.http.put<Meeting>(`${this.base}/meetings/${id}`, request);
  }

  checkAvailability(payload: {
    date: string;
    startTime: string;
    endTime: string;
    roomId?: number | null;
    participantIds: number[];
    excludeMeetingId?: number | null;
  }, priority = 'Normal'): Observable<AvailabilityResult> {
    return this.http.post<AvailabilityResult>(`${this.base}/meetings/check-availability`, payload, {
      params: new HttpParams().set('priority', priority)
    });
  }

  approveMeeting(id: number): Observable<Meeting> {
    return this.http.post<Meeting>(`${this.base}/meetings/${id}/approve`, {});
  }

  rejectMeeting(id: number, reason: string): Observable<Meeting> {
    return this.http.post<Meeting>(`${this.base}/meetings/${id}/reject`, { reason });
  }

  cancelMeeting(id: number): Observable<Meeting> {
    return this.http.post<Meeting>(`${this.base}/meetings/${id}/cancel`, {});
  }

  respondInvitation(id: number, accept: boolean): Observable<Meeting> {
    return this.http.post<Meeting>(`${this.base}/meetings/${id}/respond`, { accept });
  }

  preemptMeeting(urgentId: number, normalId: number): Observable<Meeting> {
    return this.http.post<Meeting>(`${this.base}/meetings/${urgentId}/preempt/${normalId}`, {});
  }

  // ---------- Indisponibilités ----------
  getMyUnavailabilities(): Observable<Unavailability[]> {
    return this.http.get<Unavailability[]>(`${this.base}/unavailabilities/mine`);
  }

  getAllUnavailabilities(): Observable<Unavailability[]> {
    return this.http.get<Unavailability[]>(`${this.base}/unavailabilities`);
  }

  createUnavailability(payload: Partial<Unavailability>): Observable<Unavailability> {
    return this.http.post<Unavailability>(`${this.base}/unavailabilities`, payload);
  }

  deleteUnavailability(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.base}/unavailabilities/${id}`);
  }

  // ---------- IA ----------
  aiStatus(): Observable<{ configured: boolean }> {
    return this.http.get<{ configured: boolean }>(`${this.base}/ai/status`);
  }

  analyzeText(text: string): Observable<AiProposal> {
    return this.http.post<AiProposal>(`${this.base}/ai/analyze`, { text });
  }

  generateMinutes(meetingId: number, rawNotes: string): Observable<Minutes> {
    return this.http.post<Minutes>(`${this.base}/ai/minutes`, { meetingId, rawNotes });
  }

  getMinutes(): Observable<Minutes[]> {
    return this.http.get<Minutes[]>(`${this.base}/ai/minutes`);
  }

  // ---------- Statistiques et notifications ----------
  getDashboard(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.base}/statistics/dashboard`);
  }

  getGlobalStats(): Observable<GlobalStats> {
    return this.http.get<GlobalStats>(`${this.base}/statistics/global`);
  }

  getNotifications(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(`${this.base}/notifications`);
  }

  markNotificationRead(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/notifications/${id}/read`, {});
  }
}
