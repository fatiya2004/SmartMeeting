using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface IAvailabilityService
{
    Task<List<ConflictDto>> DetectConflictsAsync(AvailabilityRequest request, MeetingPriority priority);
    Task<List<AlternativeSlotDto>> FindAlternativeSlotsAsync(AvailabilityRequest request, int maxResults = 3);
    Task<AvailabilityResultDto> CheckAsync(AvailabilityRequest request, MeetingPriority priority);
    Task<bool> IsRoomFreeAsync(int roomId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeMeetingId);
    Task<bool> IsUserFreeAsync(int userId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeMeetingId);
}

/// <summary>
/// Toute la logique de détection des conflits. C'est le coeur métier du projet :
/// aucune vérification de disponibilité n'est faite ailleurs (ni Angular, ni IA).
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    private static readonly TimeOnly WorkDayStart = new(8, 0);
    private static readonly TimeOnly WorkDayEnd = new(19, 0);
    private static readonly MeetingStatus[] BlockingStatuses =
    {
        MeetingStatus.Pending,
        MeetingStatus.Approved
    };

    private readonly AppDbContext _context;

    public AvailabilityService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Deux intervalles se chevauchent si l'un commence avant la fin de l'autre.</summary>
    public static bool Overlaps(TimeOnly startA, TimeOnly endA, TimeOnly startB, TimeOnly endB)
        => startA < endB && startB < endA;

    public async Task<AvailabilityResultDto> CheckAsync(AvailabilityRequest request, MeetingPriority priority)
    {
        var conflicts = await DetectConflictsAsync(request, priority);

        var result = new AvailabilityResultDto
        {
            IsAvailable = conflicts.Count == 0,
            Conflicts = conflicts
        };

        if (conflicts.Count > 0)
        {
            result.Alternatives = await FindAlternativeSlotsAsync(request);
        }

        return result;
    }

    public async Task<List<ConflictDto>> DetectConflictsAsync(AvailabilityRequest request, MeetingPriority priority)
    {
        var conflicts = new List<ConflictDto>();

        if (request.EndTime <= request.StartTime)
        {
            conflicts.Add(new ConflictDto
            {
                Type = "Time",
                Message = "L'heure de fin doit être postérieure à l'heure de début."
            });
            return conflicts;
        }

        // 1. Conflit de salle
        if (request.RoomId is int roomId)
        {
            var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId);

            if (room is null || !room.IsActive)
            {
                conflicts.Add(new ConflictDto { Type = "Room", Message = "Salle introuvable ou désactivée." });
            }
            else
            {
                if (request.ParticipantIds.Count + 1 > room.Capacity)
                {
                    conflicts.Add(new ConflictDto
                    {
                        Type = "Room",
                        Message = $"La salle {room.Name} accueille {room.Capacity} personnes, " +
                                  $"or la réunion en compte {request.ParticipantIds.Count + 1}."
                    });
                }

                var roomMeetings = await LoadMeetingsOfDayAsync(request.Date, request.ExcludeMeetingId);

                foreach (var meeting in roomMeetings.Where(m => m.RoomId == roomId))
                {
                    if (!Overlaps(request.StartTime, request.EndTime, meeting.StartTime, meeting.EndTime))
                    {
                        continue;
                    }

                    conflicts.Add(new ConflictDto
                    {
                        Type = "Room",
                        MeetingId = meeting.Id,
                        BlockingPriority = meeting.Priority,
                        CanBePreempted = priority == MeetingPriority.Urgent && meeting.Priority == MeetingPriority.Normal,
                        Message = $"La salle {room.Name} est déjà réservée de " +
                                  $"{meeting.StartTime:HH\\:mm} à {meeting.EndTime:HH\\:mm} " +
                                  $"pour « {meeting.Title} »."
                    });
                }
            }
        }

        if (request.ParticipantIds.Count == 0)
        {
            return conflicts;
        }

        var participants = await _context.Users.AsNoTracking()
            .Where(u => request.ParticipantIds.Contains(u.Id))
            .ToListAsync();

        // 2. Conflit de participant (déjà dans une autre réunion)
        var dayMeetings = await LoadMeetingsOfDayAsync(request.Date, request.ExcludeMeetingId);
        var meetingIds = dayMeetings.Select(m => m.Id).ToList();

        var participations = await _context.MeetingParticipants.AsNoTracking()
            .Where(mp => meetingIds.Contains(mp.MeetingId) && request.ParticipantIds.Contains(mp.UserId))
            .ToListAsync();

        foreach (var participation in participations)
        {
            var meeting = dayMeetings.First(m => m.Id == participation.MeetingId);

            if (!Overlaps(request.StartTime, request.EndTime, meeting.StartTime, meeting.EndTime))
            {
                continue;
            }

            var user = participants.FirstOrDefault(u => u.Id == participation.UserId);

            conflicts.Add(new ConflictDto
            {
                Type = "Participant",
                MeetingId = meeting.Id,
                UserId = participation.UserId,
                BlockingPriority = meeting.Priority,
                CanBePreempted = priority == MeetingPriority.Urgent && meeting.Priority == MeetingPriority.Normal,
                Message = $"{user?.FullName ?? "Un participant"} est déjà en réunion " +
                          $"({meeting.Title}) de {meeting.StartTime:HH\\:mm} à {meeting.EndTime:HH\\:mm}."
            });
        }

        // 3. Indisponibilité déclarée (garde, bloc, consultation, congé)
        var unavailabilities = await _context.Unavailabilities.AsNoTracking()
            .Where(u => u.Date == request.Date && request.ParticipantIds.Contains(u.UserId))
            .ToListAsync();

        foreach (var item in unavailabilities)
        {
            if (!Overlaps(request.StartTime, request.EndTime, item.StartTime, item.EndTime))
            {
                continue;
            }

            var user = participants.FirstOrDefault(u => u.Id == item.UserId);

            conflicts.Add(new ConflictDto
            {
                Type = "Unavailability",
                UserId = item.UserId,
                CanBePreempted = false,
                Message = $"{user?.FullName ?? "Un participant"} est indisponible " +
                          $"({item.Type}) de {item.StartTime:HH\\:mm} à {item.EndTime:HH\\:mm}."
            });
        }

        return conflicts;
    }

    public async Task<List<AlternativeSlotDto>> FindAlternativeSlotsAsync(AvailabilityRequest request, int maxResults = 3)
    {
        var alternatives = new List<AlternativeSlotDto>();
        var duration = request.EndTime - request.StartTime;

        if (duration <= TimeSpan.Zero)
        {
            return alternatives;
        }

        var rooms = await _context.Rooms.AsNoTracking()
            .Where(r => r.IsActive && r.Capacity >= request.ParticipantIds.Count + 1)
            .OrderBy(r => r.Capacity)
            .ToListAsync();

        if (rooms.Count == 0)
        {
            return alternatives;
        }

        // On explore le jour demandé puis les 4 jours suivants, par tranches de 30 minutes.
        for (var dayOffset = 0; dayOffset <= 4 && alternatives.Count < maxResults; dayOffset++)
        {
            var date = request.Date.AddDays(dayOffset);
            var slotStart = dayOffset == 0 ? WorkDayStart : WorkDayStart;

            while (slotStart.Add(duration) <= WorkDayEnd && alternatives.Count < maxResults)
            {
                var slotEnd = slotStart.Add(duration);

                var isSameSlot = dayOffset == 0
                                 && slotStart == request.StartTime
                                 && slotEnd == request.EndTime;

                if (!isSameSlot)
                {
                    foreach (var room in rooms)
                    {
                        var candidate = new AvailabilityRequest
                        {
                            Date = date,
                            StartTime = slotStart,
                            EndTime = slotEnd,
                            RoomId = room.Id,
                            ParticipantIds = request.ParticipantIds,
                            ExcludeMeetingId = request.ExcludeMeetingId
                        };

                        var conflicts = await DetectConflictsAsync(candidate, MeetingPriority.Normal);

                        if (conflicts.Count == 0)
                        {
                            alternatives.Add(new AlternativeSlotDto
                            {
                                Date = date,
                                StartTime = slotStart,
                                EndTime = slotEnd,
                                RoomId = room.Id,
                                RoomName = room.Name,
                                Justification = dayOffset == 0
                                    ? $"Même journée, tous les participants sont libres et la salle {room.Name} est disponible."
                                    : $"Premier créneau commun trouvé le {date:dd/MM/yyyy} en salle {room.Name}."
                            });
                            break;
                        }
                    }
                }

                slotStart = slotStart.AddMinutes(30);
            }
        }

        return alternatives;
    }

    public async Task<bool> IsRoomFreeAsync(int roomId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeMeetingId)
    {
        var meetings = await LoadMeetingsOfDayAsync(date, excludeMeetingId);
        return !meetings.Any(m => m.RoomId == roomId && Overlaps(start, end, m.StartTime, m.EndTime));
    }

    public async Task<bool> IsUserFreeAsync(int userId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeMeetingId)
    {
        var request = new AvailabilityRequest
        {
            Date = date,
            StartTime = start,
            EndTime = end,
            ParticipantIds = new List<int> { userId },
            ExcludeMeetingId = excludeMeetingId
        };

        var conflicts = await DetectConflictsAsync(request, MeetingPriority.Normal);
        return conflicts.Count == 0;
    }

    private async Task<List<Meeting>> LoadMeetingsOfDayAsync(DateOnly date, int? excludeMeetingId)
    {
        var query = _context.Meetings.AsNoTracking()
            .Where(m => m.Date == date && BlockingStatuses.Contains(m.Status));

        if (excludeMeetingId is int id)
        {
            query = query.Where(m => m.Id != id);
        }

        return await query.ToListAsync();
    }
}
