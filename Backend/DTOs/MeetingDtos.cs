using System.ComponentModel.DataAnnotations;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.DTOs;

public class ParticipantDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public ParticipationStatus Status { get; set; }
}

public class MeetingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public MeetingPriority Priority { get; set; }
    public MeetingStatus Status { get; set; }
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();

    public static MeetingDto From(Meeting meeting) => new()
    {
        Id = meeting.Id,
        Title = meeting.Title,
        Description = meeting.Description,
        Date = meeting.Date,
        StartTime = meeting.StartTime,
        EndTime = meeting.EndTime,
        Priority = meeting.Priority,
        Status = meeting.Status,
        RoomId = meeting.RoomId,
        RoomName = meeting.Room?.Name,
        CreatedById = meeting.CreatedById,
        CreatedByName = meeting.CreatedBy?.FullName ?? string.Empty,
        DecisionReason = meeting.DecisionReason,
        CreatedAt = meeting.CreatedAt,
        Participants = meeting.Participants
            .Select(p => new ParticipantDto
            {
                UserId = p.UserId,
                FullName = p.User?.FullName ?? string.Empty,
                Email = p.User?.Email ?? string.Empty,
                Service = p.User?.Service ?? string.Empty,
                Status = p.Status
            })
            .ToList()
    };
}

public class CreateMeetingRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    public MeetingPriority Priority { get; set; } = MeetingPriority.Normal;

    public int? RoomId { get; set; }

    public List<int> ParticipantIds { get; set; } = new();
}

public class UpdateMeetingRequest : CreateMeetingRequest
{
}

public class DecisionRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class RespondInvitationRequest
{
    public bool Accept { get; set; }
}

public class MeetingFilter
{
    public MeetingStatus? Status { get; set; }
    public MeetingPriority? Priority { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? RoomId { get; set; }
}

public class ConflictDto
{
    /// <summary>Room, Participant ou Unavailability.</summary>
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? MeetingId { get; set; }
    public int? UserId { get; set; }
    public MeetingPriority? BlockingPriority { get; set; }
    public bool CanBePreempted { get; set; }
}

public class AlternativeSlotDto
{
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
}

public class AvailabilityRequest
{
    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    public int? RoomId { get; set; }

    public List<int> ParticipantIds { get; set; } = new();

    public int? ExcludeMeetingId { get; set; }
}

public class AvailabilityResultDto
{
    public bool IsAvailable { get; set; }
    public List<ConflictDto> Conflicts { get; set; } = new();
    public List<AlternativeSlotDto> Alternatives { get; set; } = new();
}
