using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.DTOs;

public class NaturalLanguageRequest
{
    [Required, MaxLength(1000)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>Structure exacte que le LLM doit renvoyer (Structured Outputs).</summary>
public class ParsedMeetingDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; } = 60;

    [JsonPropertyName("participants")]
    public List<string> Participants { get; set; } = new();

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "NORMAL";
}

/// <summary>Réponse complète de l'assistant : analyse + vérification réelle + alternatives.</summary>
public class AiProposalDto
{
    public ParsedMeetingDto Parsed { get; set; } = new();
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public MeetingPriority Priority { get; set; }
    public List<UserDto> ResolvedParticipants { get; set; } = new();
    public List<string> UnknownParticipants { get; set; } = new();
    public int? ProposedRoomId { get; set; }
    public string? ProposedRoomName { get; set; }
    public bool IsAvailable { get; set; }
    public List<ConflictDto> Conflicts { get; set; } = new();
    public List<AlternativeSlotDto> Alternatives { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
}

public class MinutesRequest
{
    [Required]
    public int MeetingId { get; set; }

    [Required, MaxLength(5000)]
    public string RawNotes { get; set; } = string.Empty;
}

public class MinutesDto
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string MeetingTitle { get; set; } = string.Empty;
    public string RawNotes { get; set; } = string.Empty;
    public string GeneratedContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static MinutesDto From(MeetingMinutes minutes, string meetingTitle) => new()
    {
        Id = minutes.Id,
        MeetingId = minutes.MeetingId,
        MeetingTitle = meetingTitle,
        RawNotes = minutes.RawNotes,
        GeneratedContent = minutes.GeneratedContent,
        CreatedAt = minutes.CreatedAt
    };
}
