using System.ComponentModel.DataAnnotations;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.DTOs;

public class UnavailabilityDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public UnavailabilityType Type { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Reason { get; set; }

    public static UnavailabilityDto From(Unavailability item) => new()
    {
        Id = item.Id,
        UserId = item.UserId,
        UserFullName = item.User?.FullName ?? string.Empty,
        Type = item.Type,
        Date = item.Date,
        StartTime = item.StartTime,
        EndTime = item.EndTime,
        Reason = item.Reason
    };
}

public class UnavailabilityRequest
{
    public UnavailabilityType Type { get; set; } = UnavailabilityType.Autre;

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [MaxLength(300)]
    public string? Reason { get; set; }
}
