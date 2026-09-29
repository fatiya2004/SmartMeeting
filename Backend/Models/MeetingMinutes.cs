using System.ComponentModel.DataAnnotations;

namespace SmartMeeting.Api.Models;

public class MeetingMinutes
{
    public int Id { get; set; }

    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    [MaxLength(5000)]
    public string RawNotes { get; set; } = string.Empty;

    [MaxLength(8000)]
    public string GeneratedContent { get; set; } = string.Empty;

    public int CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
