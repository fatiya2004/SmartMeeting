using System.ComponentModel.DataAnnotations;

namespace SmartMeeting.Api.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Participant;

    [MaxLength(150)]
    public string Service { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Meeting> CreatedMeetings { get; set; } = new List<Meeting>();
    public ICollection<MeetingParticipant> Participations { get; set; } = new List<MeetingParticipant>();
    public ICollection<Unavailability> Unavailabilities { get; set; } = new List<Unavailability>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
