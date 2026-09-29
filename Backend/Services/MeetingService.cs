using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface IMeetingService
{
    Task<List<MeetingDto>> GetAllAsync(MeetingFilter filter);
    Task<List<MeetingDto>> GetForUserAsync(int userId);
    Task<MeetingDto?> GetByIdAsync(int id);
    Task<ServiceResult<MeetingDto>> CreateAsync(CreateMeetingRequest request, int createdById);
    Task<ServiceResult<MeetingDto>> UpdateAsync(int id, UpdateMeetingRequest request, int userId, bool isStaff);
    Task<ServiceResult<MeetingDto>> ApproveAsync(int id);
    Task<ServiceResult<MeetingDto>> RejectAsync(int id, string reason);
    Task<ServiceResult<MeetingDto>> CancelAsync(int id, int userId, bool isStaff);
    Task<ServiceResult<MeetingDto>> RespondAsync(int meetingId, int userId, bool accept);
    Task<ServiceResult<MeetingDto>> PreemptAsync(int urgentMeetingId, int normalMeetingId);
    Task<AvailabilityResultDto> CheckAvailabilityAsync(AvailabilityRequest request, MeetingPriority priority);
}

public class MeetingService : IMeetingService
{
    private readonly AppDbContext _context;
    private readonly IAvailabilityService _availabilityService;
    private readonly INotificationService _notificationService;

    public MeetingService(
        AppDbContext context,
        IAvailabilityService availabilityService,
        INotificationService notificationService)
    {
        _context = context;
        _availabilityService = availabilityService;
        _notificationService = notificationService;
    }

    public async Task<List<MeetingDto>> GetAllAsync(MeetingFilter filter)
    {
        var query = BaseQuery();

        if (filter.Status is not null) query = query.Where(m => m.Status == filter.Status);
        if (filter.Priority is not null) query = query.Where(m => m.Priority == filter.Priority);
        if (filter.From is not null) query = query.Where(m => m.Date >= filter.From);
        if (filter.To is not null) query = query.Where(m => m.Date <= filter.To);
        if (filter.RoomId is not null) query = query.Where(m => m.RoomId == filter.RoomId);

        var meetings = await query
            .OrderBy(m => m.Date).ThenBy(m => m.StartTime)
            .ToListAsync();

        return meetings.Select(MeetingDto.From).ToList();
    }

    public async Task<List<MeetingDto>> GetForUserAsync(int userId)
    {
        var meetings = await BaseQuery()
            .Where(m => m.CreatedById == userId || m.Participants.Any(p => p.UserId == userId))
            .OrderBy(m => m.Date).ThenBy(m => m.StartTime)
            .ToListAsync();

        return meetings.Select(MeetingDto.From).ToList();
    }

    public async Task<MeetingDto?> GetByIdAsync(int id)
    {
        var meeting = await BaseQuery().FirstOrDefaultAsync(m => m.Id == id);
        return meeting is null ? null : MeetingDto.From(meeting);
    }

    public Task<AvailabilityResultDto> CheckAvailabilityAsync(AvailabilityRequest request, MeetingPriority priority)
        => _availabilityService.CheckAsync(request, priority);

    public async Task<ServiceResult<MeetingDto>> CreateAsync(CreateMeetingRequest request, int createdById)
    {
        var participantIds = NormalizeParticipants(request.ParticipantIds, createdById);

        var availabilityRequest = new AvailabilityRequest
        {
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RoomId = request.RoomId,
            ParticipantIds = participantIds
        };

        // La transaction empêche deux réservations simultanées sur le même créneau.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var conflicts = await _availabilityService.DetectConflictsAsync(availabilityRequest, request.Priority);

        if (conflicts.Count > 0)
        {
            await transaction.RollbackAsync();
            return ServiceResult<MeetingDto>.Fail(string.Join(" ", conflicts.Select(c => c.Message)));
        }

        var meeting = new Meeting
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Priority = request.Priority,
            Status = MeetingStatus.Pending,
            RoomId = request.RoomId,
            CreatedById = createdById
        };

        foreach (var userId in participantIds)
        {
            meeting.Participants.Add(new MeetingParticipant { UserId = userId });
        }

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var created = await BaseQuery().FirstAsync(m => m.Id == meeting.Id);
        await _notificationService.NotifyInvitationAsync(created);

        return ServiceResult<MeetingDto>.Ok(MeetingDto.From(created));
    }

    public async Task<ServiceResult<MeetingDto>> UpdateAsync(int id, UpdateMeetingRequest request, int userId, bool isStaff)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting is null)
        {
            return ServiceResult<MeetingDto>.Fail("Réunion introuvable.");
        }

        if (!isStaff && meeting.CreatedById != userId)
        {
            return ServiceResult<MeetingDto>.Fail("Vous ne pouvez modifier que vos propres réunions.");
        }

        var participantIds = NormalizeParticipants(request.ParticipantIds, meeting.CreatedById);

        var conflicts = await _availabilityService.DetectConflictsAsync(new AvailabilityRequest
        {
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RoomId = request.RoomId,
            ParticipantIds = participantIds,
            ExcludeMeetingId = id
        }, request.Priority);

        if (conflicts.Count > 0)
        {
            return ServiceResult<MeetingDto>.Fail(string.Join(" ", conflicts.Select(c => c.Message)));
        }

        meeting.Title = request.Title.Trim();
        meeting.Description = request.Description.Trim();
        meeting.Date = request.Date;
        meeting.StartTime = request.StartTime;
        meeting.EndTime = request.EndTime;
        meeting.Priority = request.Priority;
        meeting.RoomId = request.RoomId;

        var existing = meeting.Participants.ToList();

        foreach (var participant in existing.Where(p => !participantIds.Contains(p.UserId)))
        {
            meeting.Participants.Remove(participant);
        }

        foreach (var newId in participantIds.Where(pid => existing.All(p => p.UserId != pid)))
        {
            meeting.Participants.Add(new MeetingParticipant { UserId = newId });
        }

        await _context.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(m => m.Id == id);
        await _notificationService.NotifyModifiedAsync(updated);

        return ServiceResult<MeetingDto>.Ok(MeetingDto.From(updated));
    }

    public async Task<ServiceResult<MeetingDto>> ApproveAsync(int id)
    {
        var meeting = await _context.Meetings.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == id);

        if (meeting is null)
        {
            return ServiceResult<MeetingDto>.Fail("Réunion introuvable.");
        }

        if (meeting.Status != MeetingStatus.Pending)
        {
            return ServiceResult<MeetingDto>.Fail("Seule une demande en attente peut être validée.");
        }

        // Nouvelle vérification au moment de la validation : la situation a pu changer.
        var conflicts = await _availabilityService.DetectConflictsAsync(new AvailabilityRequest
        {
            Date = meeting.Date,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            RoomId = meeting.RoomId,
            ParticipantIds = meeting.Participants.Select(p => p.UserId).ToList(),
            ExcludeMeetingId = meeting.Id
        }, meeting.Priority);

        if (conflicts.Count > 0)
        {
            return ServiceResult<MeetingDto>.Fail(string.Join(" ", conflicts.Select(c => c.Message)));
        }

        meeting.Status = MeetingStatus.Approved;
        await _context.SaveChangesAsync();

        var approved = await BaseQuery().FirstAsync(m => m.Id == id);
        await _notificationService.NotifyApprovedAsync(approved);

        return ServiceResult<MeetingDto>.Ok(MeetingDto.From(approved));
    }

    public async Task<ServiceResult<MeetingDto>> RejectAsync(int id, string reason)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == id);

        if (meeting is null)
        {
            return ServiceResult<MeetingDto>.Fail("Réunion introuvable.");
        }

        meeting.Status = MeetingStatus.Rejected;
        meeting.DecisionReason = string.IsNullOrWhiteSpace(reason) ? "Demande refusée par le secrétariat." : reason;
        await _context.SaveChangesAsync();

        var rejected = await BaseQuery().FirstAsync(m => m.Id == id);
        await _notificationService.NotifyRejectedAsync(rejected, meeting.DecisionReason!);

        return ServiceResult<MeetingDto>.Ok(MeetingDto.From(rejected));
    }

    public async Task<ServiceResult<MeetingDto>> CancelAsync(int id, int userId, bool isStaff)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == id);

        if (meeting is null)
        {
            return ServiceResult<MeetingDto>.Fail("Réunion introuvable.");
        }

        if (!isStaff && meeting.CreatedById != userId)
        {
            return ServiceResult<MeetingDto>.Fail("Vous ne pouvez annuler que vos propres réunions.");
        }

        meeting.Status = MeetingStatus.Cancelled;
        await _context.SaveChangesAsync();

        var cancelled = await BaseQuery().FirstAsync(m => m.Id == id);
        await _notificationService.NotifyModifiedAsync(cancelled);

        return ServiceResult<MeetingDto>.Ok(MeetingDto.From(cancelled));
    }

    public async Task<ServiceResult<MeetingDto>> RespondAsync(int meetingId, int userId, bool accept)
    {
        var participation = await _context.MeetingParticipants
            .FirstOrDefaultAsync(mp => mp.MeetingId == meetingId && mp.UserId == userId);

        if (participation is null)
        {
            return ServiceResult<MeetingDto>.Fail("Vous n'êtes pas invité à cette réunion.");
        }

        participation.Status = accept ? ParticipationStatus.Accepted : ParticipationStatus.Declined;
        participation.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var meeting = await BaseQuery().FirstAsync(m => m.Id == meetingId);
        return ServiceResult<MeetingDto>.Ok(MeetingDto.From(meeting));
    }

    /// <summary>
    /// Réunion urgente : on déplace la réunion normale vers le premier créneau libre,
    /// puis on valide la réunion urgente. La secrétaire déclenche toujours cette action.
    /// </summary>
    public async Task<ServiceResult<MeetingDto>> PreemptAsync(int urgentMeetingId, int normalMeetingId)
    {
        var urgent = await _context.Meetings.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == urgentMeetingId);
        var normal = await _context.Meetings.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == normalMeetingId);

        if (urgent is null || normal is null)
        {
            return ServiceResult<MeetingDto>.Fail("Réunion introuvable.");
        }

        if (urgent.Priority != MeetingPriority.Urgent)
        {
            return ServiceResult<MeetingDto>.Fail("Seule une réunion urgente peut déplacer une autre réunion.");
        }

        if (normal.Priority != MeetingPriority.Normal)
        {
            return ServiceResult<MeetingDto>.Fail("Seule une réunion de priorité normale peut être déplacée.");
        }

        var alternatives = await _availabilityService.FindAlternativeSlotsAsync(new AvailabilityRequest
        {
            Date = normal.Date,
            StartTime = normal.StartTime,
            EndTime = normal.EndTime,
            RoomId = normal.RoomId,
            ParticipantIds = normal.Participants.Select(p => p.UserId).ToList(),
            ExcludeMeetingId = normal.Id
        }, maxResults: 1);

        if (alternatives.Count == 0)
        {
            normal.Status = MeetingStatus.Cancelled;
            normal.DecisionReason = "Annulée au profit d'une réunion urgente, aucun créneau alternatif disponible.";
        }
        else
        {
            var slot = alternatives[0];
            normal.Date = slot.Date;
            normal.StartTime = slot.StartTime;
            normal.EndTime = slot.EndTime;
            normal.RoomId = slot.RoomId;
            normal.DecisionReason = "Déplacée au profit d'une réunion urgente.";
        }

        await _context.SaveChangesAsync();

        var movedMeeting = await BaseQuery().FirstAsync(m => m.Id == normal.Id);
        await _notificationService.NotifyModifiedAsync(movedMeeting);

        return await ApproveAsync(urgent.Id);
    }

    private IQueryable<Meeting> BaseQuery() => _context.Meetings
        .Include(m => m.Room)
        .Include(m => m.CreatedBy)
        .Include(m => m.Participants).ThenInclude(p => p.User)
        .AsQueryable();

    private static List<int> NormalizeParticipants(IEnumerable<int> ids, int createdById)
    {
        var list = ids.Distinct().ToList();

        if (!list.Contains(createdById))
        {
            list.Add(createdById);
        }

        return list;
    }
}
