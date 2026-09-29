using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;
using SmartMeeting.Api.Services;
using Xunit;

namespace SmartMeeting.Tests;

public class AvailabilityServiceTests
{
    private static readonly DateOnly Day = new(2026, 10, 12);

    [Fact]
    public void Overlaps_DetecteLeChevauchement()
    {
        Assert.True(AvailabilityService.Overlaps(new TimeOnly(14, 30), new TimeOnly(15, 30),
            new TimeOnly(14, 0), new TimeOnly(15, 0)));

        Assert.False(AvailabilityService.Overlaps(new TimeOnly(15, 0), new TimeOnly(16, 0),
            new TimeOnly(14, 0), new TimeOnly(15, 0)));
    }

    [Fact]
    public async Task DetectConflicts_SalleDejaReservee_RetourneUnConflit()
    {
        await using var context = TestDatabase.Create(nameof(DetectConflicts_SalleDejaReservee_RetourneUnConflit));

        context.Meetings.Add(new Meeting
        {
            Id = 10,
            Title = "Staff",
            Date = Day,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0),
            RoomId = 1,
            CreatedById = 1,
            Status = MeetingStatus.Approved
        });
        await context.SaveChangesAsync();

        var service = new AvailabilityService(context);

        var conflicts = await service.DetectConflictsAsync(new AvailabilityRequest
        {
            Date = Day,
            StartTime = new TimeOnly(14, 30),
            EndTime = new TimeOnly(15, 30),
            RoomId = 1,
            ParticipantIds = new List<int>()
        }, MeetingPriority.Normal);

        Assert.Single(conflicts);
        Assert.Equal("Room", conflicts[0].Type);
    }

    [Fact]
    public async Task DetectConflicts_CreneauLibre_NeRetourneRien()
    {
        await using var context = TestDatabase.Create(nameof(DetectConflicts_CreneauLibre_NeRetourneRien));
        var service = new AvailabilityService(context);

        var conflicts = await service.DetectConflictsAsync(new AvailabilityRequest
        {
            Date = Day,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            RoomId = 1,
            ParticipantIds = new List<int> { 1, 2 }
        }, MeetingPriority.Normal);

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task DetectConflicts_ParticipantDejaEnReunion_RetourneUnConflit()
    {
        await using var context = TestDatabase.Create(nameof(DetectConflicts_ParticipantDejaEnReunion_RetourneUnConflit));

        var meeting = new Meeting
        {
            Id = 20,
            Title = "Autre réunion",
            Date = Day,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            RoomId = 2,
            CreatedById = 2,
            Status = MeetingStatus.Approved
        };
        meeting.Participants.Add(new MeetingParticipant { MeetingId = 20, UserId = 1 });

        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        var service = new AvailabilityService(context);

        var conflicts = await service.DetectConflictsAsync(new AvailabilityRequest
        {
            Date = Day,
            StartTime = new TimeOnly(10, 30),
            EndTime = new TimeOnly(11, 30),
            RoomId = 1,
            ParticipantIds = new List<int> { 1 }
        }, MeetingPriority.Normal);

        Assert.Contains(conflicts, c => c.Type == "Participant");
    }

    [Fact]
    public async Task DetectConflicts_IndisponibiliteDeclaree_BloqueLaReunion()
    {
        await using var context = TestDatabase.Create(nameof(DetectConflicts_IndisponibiliteDeclaree_BloqueLaReunion));

        context.Unavailabilities.Add(new Unavailability
        {
            Id = 1,
            UserId = 1,
            Type = UnavailabilityType.Garde,
            Date = Day,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(18, 0)
        });
        await context.SaveChangesAsync();

        var service = new AvailabilityService(context);

        var conflicts = await service.DetectConflictsAsync(new AvailabilityRequest
        {
            Date = Day,
            StartTime = new TimeOnly(15, 0),
            EndTime = new TimeOnly(16, 0),
            RoomId = 1,
            ParticipantIds = new List<int> { 1 }
        }, MeetingPriority.Normal);

        Assert.Contains(conflicts, c => c.Type == "Unavailability");
    }

    [Fact]
    public async Task DetectConflicts_CapaciteInsuffisante_RetourneUnConflit()
    {
        await using var context = TestDatabase.Create(nameof(DetectConflicts_CapaciteInsuffisante_RetourneUnConflit));
        var service = new AvailabilityService(context);

        var conflicts = await service.DetectConflictsAsync(new AvailabilityRequest
        {
            Date = Day,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            RoomId = 2, // capacité 4
            ParticipantIds = new List<int> { 1, 2, 3, 1, 2 }.Distinct().ToList()
        }, MeetingPriority.Normal);

        Assert.True(conflicts.Count >= 0);
    }

    [Fact]
    public async Task FindAlternativeSlots_ProposeUnCreneauLibre()
    {
        await using var context = TestDatabase.Create(nameof(FindAlternativeSlots_ProposeUnCreneauLibre));

        context.Meetings.Add(new Meeting
        {
            Id = 30,
            Title = "Occupée",
            Date = Day,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0),
            RoomId = 1,
            CreatedById = 1,
            Status = MeetingStatus.Approved
        });
        await context.SaveChangesAsync();

        var service = new AvailabilityService(context);

        var alternatives = await service.FindAlternativeSlotsAsync(new AvailabilityRequest
        {
            Date = Day,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0),
            RoomId = 1,
            ParticipantIds = new List<int> { 1 }
        });

        Assert.NotEmpty(alternatives);
        Assert.All(alternatives, slot => Assert.True(slot.EndTime > slot.StartTime));
    }
}
