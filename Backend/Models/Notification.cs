using System.ComponentModel.DataAnnotations;

namespace SmartMeeting.Api.Models;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public NotificationType Type { get; set; }

    [MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public bool IsSent { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }
}
