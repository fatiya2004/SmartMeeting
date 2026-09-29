using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface IStatisticsService
{
    Task<DashboardStatsDto> GetDashboardAsync(int userId, bool isStaff);
    Task<GlobalStatsDto> GetGlobalAsync();
}

public class StatisticsService : IStatisticsService
{
    /// <summary>Temps moyen économisé par réunion traitée automatiquement (minutes).</summary>
    private const int MinutesSavedPerMeeting = 20;

    private readonly AppDbContext _context;

    public StatisticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetDashboardAsync(int userId, bool isStaff)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekEnd = today.AddDays(7);

        var query = _context.Meetings
            .Include(m => m.Room)
            .Include(m => m.CreatedBy)
            .Include(m => m.Participants).ThenInclude(p => p.User)
            .AsQueryable();

        if (!isStaff)
        {
            query = query.Where(m => m.CreatedById == userId || m.Participants.Any(p => p.UserId == userId));
        }

        var meetings = await query.ToListAsync();
        var activeRooms = await _context.Rooms.CountAsync(r => r.IsActive);

        var todayApproved = meetings.Count(m => m.Date == today && m.Status == MeetingStatus.Approved);
        var occupancy = activeRooms == 0 ? 0 : Math.Round(todayApproved * 100.0 / (activeRooms * 6), 1);

        return new DashboardStatsDto
        {
            MeetingsToday = meetings.Count(m => m.Date == today && m.Status != MeetingStatus.Cancelled),
            MeetingsThisWeek = meetings.Count(m => m.Date >= today && m.Date < weekEnd && m.Status != MeetingStatus.Cancelled),
            PendingRequests = meetings.Count(m => m.Status == MeetingStatus.Pending),
            UrgentMeetings = meetings.Count(m => m.Priority == MeetingPriority.Urgent && m.Status != MeetingStatus.Cancelled),
            ActiveRooms = activeRooms,
            RoomOccupancyRate = Math.Min(occupancy, 100),
            UpcomingMeetings = meetings
                .Where(m => m.Date >= today && m.Status == MeetingStatus.Approved)
                .OrderBy(m => m.Date).ThenBy(m => m.StartTime)
                .Take(5)
                .Select(MeetingDto.From)
                .ToList()
        };
    }

    public async Task<GlobalStatsDto> GetGlobalAsync()
    {
        var meetings = await _context.Meetings.AsNoTracking().ToListAsync();
        var rooms = await _context.Rooms.AsNoTracking().ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var perDay = Enumerable.Range(-6, 7)
            .Select(offset =>
            {
                var date = today.AddDays(offset);
                return new LabelValueDto
                {
                    Label = date.ToString("dd/MM"),
                    Value = meetings.Count(m => m.Date == date && m.Status != MeetingStatus.Cancelled)
                };
            })
            .ToList();

        var perPriority = Enum.GetValues<MeetingPriority>()
            .Select(priority => new LabelValueDto
            {
                Label = priority.ToString(),
                Value = meetings.Count(m => m.Priority == priority)
            })
            .ToList();

        var occupancy = rooms.Select(room =>
        {
            var hours = meetings
                .Where(m => m.RoomId == room.Id && m.Status == MeetingStatus.Approved)
                .Sum(m => (m.EndTime - m.StartTime).TotalHours);

            return new LabelValueDto { Label = room.Name, Value = Math.Round(hours, 1) };
        }).ToList();

        var approved = meetings.Count(m => m.Status == MeetingStatus.Approved);

        return new GlobalStatsDto
        {
            TotalMeetings = meetings.Count,
            Approved = approved,
            Rejected = meetings.Count(m => m.Status == MeetingStatus.Rejected),
            Pending = meetings.Count(m => m.Status == MeetingStatus.Pending),
            EstimatedHoursSaved = Math.Round(meetings.Count * MinutesSavedPerMeeting / 60.0, 1),
            MeetingsPerDay = perDay,
            MeetingsPerPriority = perPriority,
            RoomOccupancy = occupancy
        };
    }
}
