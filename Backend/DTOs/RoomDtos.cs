using System.ComponentModel.DataAnnotations;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.DTOs;

public class RoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public static RoomDto From(Room room) => new()
    {
        Id = room.Id,
        Name = room.Name,
        Capacity = room.Capacity,
        Location = room.Location,
        Equipment = room.Equipment,
        IsActive = room.IsActive
    };
}

public class RoomRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Capacity { get; set; }

    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Equipment { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
