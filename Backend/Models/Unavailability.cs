using System.ComponentModel.DataAnnotations;

namespace SmartMeeting.Api.Models;

public class Unavailability
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public UnavailabilityType Type { get; set; } = UnavailabilityType.Autre;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    [MaxLength(300)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
