using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.Email;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface INotificationService
{
    Task NotifyInvitationAsync(Meeting meeting);
    Task NotifyApprovedAsync(Meeting meeting);
    Task NotifyRejectedAsync(Meeting meeting, string reason);
    Task NotifyModifiedAsync(Meeting meeting);
    Task NotifyReminderAsync(Meeting meeting);
}

/// <summary>
/// Enregistre la notification en base puis envoie l'email correspondant.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public NotificationService(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public Task NotifyInvitationAsync(Meeting meeting) =>
        SendToParticipantsAsync(meeting, NotificationType.Invitation,
            (user, room) => EmailTemplates.Invitation(user, meeting, room));

    public Task NotifyApprovedAsync(Meeting meeting) =>
        SendToEveryoneAsync(meeting, NotificationType.Confirmation,
            (user, room) => EmailTemplates.Approved(user, meeting, room));

    public async Task NotifyRejectedAsync(Meeting meeting, string reason)
    {
        var creator = await _context.Users.FirstOrDefaultAsync(u => u.Id == meeting.CreatedById);
        if (creator is null) return;

        var message = EmailTemplates.Rejected(creator, meeting, reason);
        await SendOneAsync(creator, meeting, NotificationType.Refus, message);
    }

    public Task NotifyModifiedAsync(Meeting meeting) =>
        SendToEveryoneAsync(meeting, NotificationType.Modification,
            (user, room) => EmailTemplates.Modified(user, meeting, room));

    public Task NotifyReminderAsync(Meeting meeting) =>
        SendToEveryoneAsync(meeting, NotificationType.Rappel,
            (user, room) => EmailTemplates.Reminder(user, meeting, room));

    private async Task SendToParticipantsAsync(Meeting meeting, NotificationType type,
        Func<User, string, EmailMessage> builder)
    {
        var recipients = await LoadParticipantsAsync(meeting.Id);
        var roomName = await LoadRoomNameAsync(meeting.RoomId);

        foreach (var user in recipients)
        {
            await SendOneAsync(user, meeting, type, builder(user, roomName));
        }
    }

    private async Task SendToEveryoneAsync(Meeting meeting, NotificationType type,
        Func<User, string, EmailMessage> builder)
    {
        var recipients = await LoadParticipantsAsync(meeting.Id);
        var creator = await _context.Users.FirstOrDefaultAsync(u => u.Id == meeting.CreatedById);

        if (creator is not null && recipients.All(r => r.Id != creator.Id))
        {
            recipients.Add(creator);
        }

        var roomName = await LoadRoomNameAsync(meeting.RoomId);

        foreach (var user in recipients)
        {
            await SendOneAsync(user, meeting, type, builder(user, roomName));
        }
    }

    private async Task SendOneAsync(User user, Meeting meeting, NotificationType type, EmailMessage message)
    {
        var notification = new Notification
        {
            UserId = user.Id,
            MeetingId = meeting.Id,
            Type = type,
            Subject = message.Subject,
            Message = $"{meeting.Title} — {meeting.Date:dd/MM/yyyy} {meeting.StartTime:HH\\:mm}"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var sent = await _emailService.SendAsync(message);

        notification.IsSent = sent;
        notification.SentAt = sent ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();
    }

    private async Task<List<User>> LoadParticipantsAsync(int meetingId) =>
        await _context.MeetingParticipants
            .Where(mp => mp.MeetingId == meetingId)
            .Select(mp => mp.User)
            .ToListAsync();

    private async Task<string> LoadRoomNameAsync(int? roomId)
    {
        if (roomId is null) return "À définir";

        var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId);
        return room?.Name ?? "À définir";
    }
}
