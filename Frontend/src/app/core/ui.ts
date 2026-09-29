import { MeetingPriority, MeetingStatus } from './models';

export const statusLabels: Record<MeetingStatus, string> = {
  Pending: 'En attente',
  Approved: 'Approuvée',
  Rejected: 'Refusée',
  Cancelled: 'Annulée',
  Completed: 'Terminée'
};

export const statusClasses: Record<MeetingStatus, string> = {
  Pending: 'badge badge-pending',
  Approved: 'badge badge-approved',
  Rejected: 'badge badge-rejected',
  Cancelled: 'badge badge-cancelled',
  Completed: 'badge badge-completed'
};

export const priorityLabels: Record<MeetingPriority, string> = {
  Normal: 'Normale',
  High: 'Haute',
  Urgent: 'Urgente'
};

export const priorityClasses: Record<MeetingPriority, string> = {
  Normal: 'badge badge-normal',
  High: 'badge badge-high',
  Urgent: 'badge badge-urgent'
};

export function shortTime(value: string): string {
  return value ? value.substring(0, 5) : '';
}

export function frDate(value: string): string {
  if (!value) {
    return '';
  }
  const [year, month, day] = value.split('-');
  return `${day}/${month}/${year}`;
}
