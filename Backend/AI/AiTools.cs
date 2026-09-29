using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;
using SmartMeeting.Api.Services;

namespace SmartMeeting.Api.AI;

/// <summary>
/// Les seuls "outils" que l'IA peut demander au backend d'exécuter (Tool Calling).
/// L'IA ne voit jamais la base de données : elle demande, le backend exécute et contrôle.
/// </summary>
public interface IAiToolExecutor
{
    Task<bool> CheckRoomAvailabilityAsync(int roomId, DateOnly date, TimeOnly start, TimeOnly end);
    Task<bool> CheckParticipantAvailabilityAsync(int userId, DateOnly date, TimeOnly start, TimeOnly end);
    Task<List<AlternativeSlotDto>> SuggestAlternativeSlotsAsync(AvailabilityRequest request);
    Task<int?> FindBestRoomAsync(DateOnly date, TimeOnly start, TimeOnly end, int attendeeCount);
    Task<ServiceResult<MeetingDto>> CreateMeetingAsync(CreateMeetingRequest request, int createdById);
}

public class AiToolExecutor : IAiToolExecutor
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IMeetingService _meetingService;
    private readonly IRoomService _roomService;

    public AiToolExecutor(
        IAvailabilityService availabilityService,
        IMeetingService meetingService,
        IRoomService roomService)
    {
        _availabilityService = availabilityService;
        _meetingService = meetingService;
        _roomService = roomService;
    }

    public Task<bool> CheckRoomAvailabilityAsync(int roomId, DateOnly date, TimeOnly start, TimeOnly end)
        => _availabilityService.IsRoomFreeAsync(roomId, date, start, end, null);

    public Task<bool> CheckParticipantAvailabilityAsync(int userId, DateOnly date, TimeOnly start, TimeOnly end)
        => _availabilityService.IsUserFreeAsync(userId, date, start, end, null);

    public Task<List<AlternativeSlotDto>> SuggestAlternativeSlotsAsync(AvailabilityRequest request)
        => _availabilityService.FindAlternativeSlotsAsync(request);

    public async Task<int?> FindBestRoomAsync(DateOnly date, TimeOnly start, TimeOnly end, int attendeeCount)
    {
        var rooms = await _roomService.GetAllAsync(onlyActive: true);

        foreach (var room in rooms.Where(r => r.Capacity >= attendeeCount).OrderBy(r => r.Capacity))
        {
            if (await _availabilityService.IsRoomFreeAsync(room.Id, date, start, end, null))
            {
                return room.Id;
            }
        }

        return null;
    }

    public Task<ServiceResult<MeetingDto>> CreateMeetingAsync(CreateMeetingRequest request, int createdById)
        => _meetingService.CreateAsync(request, createdById);
}
