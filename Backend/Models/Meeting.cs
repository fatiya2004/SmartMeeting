using System.ComponentModel.DataAnnotations;

namespace SmartMeeting.Api.Models;

public class Meeting
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public MeetingPriority Priority { get; set; } = MeetingPriority.Normal;

    public MeetingStatus Status { get; set; } = MeetingStatus.Pending;

    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    [MaxLength(500)]
    public string? DecisionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();

    public MeetingMinutes? Minutes { get; set; }
}
