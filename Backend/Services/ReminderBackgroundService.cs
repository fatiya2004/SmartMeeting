using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

/// <summary>
/// Tâche de fond simple : envoie un rappel une heure avant chaque réunion approuvée.
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromMinutes(60);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceProvider serviceProvider, ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi des rappels.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var limit = TimeOnly.FromDateTime(now.Add(ReminderWindow));
        var current = TimeOnly.FromDateTime(now);

        var meetings = await context.Meetings
            .Include(m => m.Room)
            .Include(m => m.CreatedBy)
            .Include(m => m.Participants).ThenInclude(p => p.User)
            .Where(m => m.Status == MeetingStatus.Approved
                        && m.Date == today
                        && m.StartTime > current
                        && m.StartTime <= limit)
            .ToListAsync(cancellationToken);

        foreach (var meeting in meetings)
        {
            var alreadySent = await context.Notifications.AnyAsync(
                n => n.MeetingId == meeting.Id && n.Type == NotificationType.Rappel,
                cancellationToken);

            if (alreadySent) continue;

            await notificationService.NotifyReminderAsync(meeting);
            _logger.LogInformation("Rappel envoyé pour la réunion {MeetingId}", meeting.Id);
        }
    }
}
