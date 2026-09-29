export type UserRole = 'Admin' | 'Secretaire' | 'Participant';
export type MeetingStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled' | 'Completed';
export type MeetingPriority = 'Normal' | 'High' | 'Urgent';
export type ParticipationStatus = 'Pending' | 'Accepted' | 'Declined';
export type UnavailabilityType = 'Garde' | 'Consultation' | 'BlocOperatoire' | 'Conge' | 'Autre';

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  role: UserRole;
  service: string;
  isActive: boolean;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface Room {
  id: number;
  name: string;
  capacity: number;
  location: string;
  equipment: string;
  isActive: boolean;
}

export interface Participant {
  userId: number;
  fullName: string;
  email: string;
  service: string;
  status: ParticipationStatus;
}

export interface Meeting {
  id: number;
  title: string;
  description: string;
  date: string;
  startTime: string;
  endTime: string;
  priority: MeetingPriority;
  status: MeetingStatus;
  roomId?: number | null;
  roomName?: string | null;
  createdById: number;
  createdByName: string;
  decisionReason?: string | null;
  createdAt: string;
  participants: Participant[];
}

export interface MeetingRequest {
  title: string;
  description: string;
  date: string;
  startTime: string;
  endTime: string;
  priority: MeetingPriority;
  roomId?: number | null;
  participantIds: number[];
}

export interface Conflict {
  type: string;
  message: string;
  meetingId?: number | null;
  userId?: number | null;
  blockingPriority?: MeetingPriority | null;
  canBePreempted: boolean;
}

export interface AlternativeSlot {
  date: string;
  startTime: string;
  endTime: string;
  roomId: number;
  roomName: string;
  justification: string;
}

export interface AvailabilityResult {
  isAvailable: boolean;
  conflicts: Conflict[];
  alternatives: AlternativeSlot[];
}

export interface Unavailability {
  id: number;
  userId: number;
  userFullName: string;
  type: UnavailabilityType;
  date: string;
  startTime: string;
  endTime: string;
  reason?: string | null;
}

export interface AiProposal {
  parsed: {
    title: string;
    date: string;
    startTime: string;
    durationMinutes: number;
    participants: string[];
    priority: string;
  };
  date: string;
  startTime: string;
  endTime: string;
  priority: MeetingPriority;
  resolvedParticipants: User[];
  unknownParticipants: string[];
  proposedRoomId?: number | null;
  proposedRoomName?: string | null;
  isAvailable: boolean;
  conflicts: Conflict[];
  alternatives: AlternativeSlot[];
  explanation: string;
}

export interface Minutes {
  id: number;
  meetingId: number;
  meetingTitle: string;
  rawNotes: string;
  generatedContent: string;
  createdAt: string;
}

export interface DashboardStats {
  meetingsToday: number;
  meetingsThisWeek: number;
  pendingRequests: number;
  urgentMeetings: number;
  activeRooms: number;
  roomOccupancyRate: number;
  upcomingMeetings: Meeting[];
}

export interface LabelValue {
  label: string;
  value: number;
}

export interface GlobalStats {
  totalMeetings: number;
  approved: number;
  rejected: number;
  pending: number;
  estimatedHoursSaved: number;
  meetingsPerDay: LabelValue[];
  meetingsPerPriority: LabelValue[];
  roomOccupancy: LabelValue[];
}

export interface AppNotification {
  id: number;
  type: string;
  subject: string;
  message: string;
  isRead: boolean;
  isSent: boolean;
  createdAt: string;
  meetingId?: number | null;
}
