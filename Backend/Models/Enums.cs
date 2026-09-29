namespace SmartMeeting.Api.Models;

public enum UserRole
{
    Admin = 0,
    Secretaire = 1,
    Participant = 2
}

public enum MeetingStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Completed = 4
}

public enum MeetingPriority
{
    Normal = 0,
    High = 1,
    Urgent = 2
}

public enum ParticipationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2
}

public enum UnavailabilityType
{
    Garde = 0,
    Consultation = 1,
    BlocOperatoire = 2,
    Conge = 3,
    Autre = 4
}

public enum NotificationType
{
    Invitation = 0,
    Confirmation = 1,
    Refus = 2,
    Modification = 3,
    Rappel = 4
}
