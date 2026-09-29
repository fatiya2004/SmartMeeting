using System.ComponentModel.DataAnnotations;

namespace SmartMeeting.Api.Models;

public class Room
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Equipment { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
