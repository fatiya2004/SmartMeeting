using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;
using SmartMeeting.Api.Services;

namespace SmartMeeting.Api.AI;

public interface IAiAssistantService
{
    bool IsConfigured { get; }
    Task<ServiceResult<AiProposalDto>> AnalyzeAsync(string text, int requesterId, CancellationToken cancellationToken = default);
    Task<ServiceResult<MinutesDto>> GenerateMinutesAsync(MinutesRequest request, int userId, CancellationToken cancellationToken = default);
    Task<List<MinutesDto>> GetMinutesAsync(int? meetingId);
}

/// <summary>
/// Orchestration de l'IA :
/// 1. Le LLM transforme le texte libre en JSON (Structured Outputs).
/// 2. Le backend exécute les outils (Tool Calling) pour vérifier la réalité.
/// 3. Le LLM rédige uniquement l'explication finale.
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly ILlmClient _llmClient;
    private readonly IAiToolExecutor _tools;
    private readonly IUserService _userService;
    private readonly IAvailabilityService _availabilityService;
    private readonly AppDbContext _context;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        ILlmClient llmClient,
        IAiToolExecutor tools,
        IUserService userService,
        IAvailabilityService availabilityService,
        AppDbContext context,
        ILogger<AiAssistantService> logger)
    {
        _llmClient = llmClient;
        _tools = tools;
        _userService = userService;
        _availabilityService = availabilityService;
        _context = context;
        _logger = logger;
    }

    public bool IsConfigured => _llmClient.IsConfigured;

    public async Task<ServiceResult<AiProposalDto>> AnalyzeAsync(string text, int requesterId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return ServiceResult<AiProposalDto>.Fail(
                "Le service IA n'est pas configuré. Renseignez LLM_API_KEY dans le fichier .env.");
        }

        ParsedMeetingDto parsed;

        try
        {
            var userPrompt = $"Date du jour : {DateTime.Now:yyyy-MM-dd} ({DateTime.Now:dddd}).\nDemande : {text}";

            var json = await _llmClient.CompleteJsonAsync(
                AiPrompts.MeetingSystemPrompt,
                userPrompt,
                "meeting_request",
                AiPrompts.MeetingJsonSchema,
                cancellationToken);

            parsed = JsonSerializer.Deserialize<ParsedMeetingDto>(json)
                     ?? throw new InvalidOperationException("Réponse IA vide.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analyse IA impossible");
            return ServiceResult<AiProposalDto>.Fail("L'assistant n'a pas pu analyser la demande. Réessayez ou utilisez le formulaire classique.");
        }

        // ---- Normalisation des données renvoyées par le LLM ----
        if (!DateOnly.TryParse(parsed.Date, CultureInfo.InvariantCulture, out var date))
        {
            date = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        }

        if (!TimeOnly.TryParse(parsed.StartTime, CultureInfo.InvariantCulture, out var startTime))
        {
            startTime = new TimeOnly(9, 0);
        }

        var duration = parsed.DurationMinutes is > 0 and <= 480 ? parsed.DurationMinutes : 60;
        var endTime = startTime.AddMinutes(duration);

        var priority = parsed.Priority.ToUpperInvariant() switch
        {
            "URGENT" => MeetingPriority.Urgent,
            "HIGH" => MeetingPriority.High,
            _ => MeetingPriority.Normal
        };

        // ---- Résolution des participants (outil backend) ----
        var resolved = await _userService.SearchByNameAsync(parsed.Participants);
        var unknown = parsed.Participants
            .Where(name => resolved.All(u => !u.FullName.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var participantIds = resolved.Select(u => u.Id).ToList();

        if (!participantIds.Contains(requesterId))
        {
            participantIds.Add(requesterId);
        }

        // ---- Outil : trouver la meilleure salle ----
        var roomId = await _tools.FindBestRoomAsync(date, startTime, endTime, participantIds.Count);
        var roomName = roomId is null
            ? null
            : (await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken))?.Name;

        // ---- Outils : vérification réelle des disponibilités ----
        var availabilityRequest = new AvailabilityRequest
        {
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            RoomId = roomId,
            ParticipantIds = participantIds
        };

        var check = await _availabilityService.CheckAsync(availabilityRequest, priority);

        var proposal = new AiProposalDto
        {
            Parsed = parsed,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            Priority = priority,
            ResolvedParticipants = resolved,
            UnknownParticipants = unknown,
            ProposedRoomId = roomId,
            ProposedRoomName = roomName,
            IsAvailable = check.IsAvailable,
            Conflicts = check.Conflicts,
            Alternatives = check.Alternatives
        };

        proposal.Explanation = await BuildExplanationAsync(proposal, cancellationToken);

        return ServiceResult<AiProposalDto>.Ok(proposal);
    }

    public async Task<ServiceResult<MinutesDto>> GenerateMinutesAsync(MinutesRequest request, int userId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return ServiceResult<MinutesDto>.Fail("Le service IA n'est pas configuré.");
        }

        var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == request.MeetingId, cancellationToken);

        if (meeting is null)
        {
            return ServiceResult<MinutesDto>.Fail("Réunion introuvable.");
        }

        string generated;

        try
        {
            generated = await _llmClient.CompleteTextAsync(
                AiPrompts.MinutesSystemPrompt,
                $"Réunion : {meeting.Title} ({meeting.Date:dd/MM/yyyy}).\nNotes brutes :\n{request.RawNotes}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Génération du compte rendu impossible");
            return ServiceResult<MinutesDto>.Fail("La génération du compte rendu a échoué.");
        }

        var existing = await _context.MeetingMinutes.FirstOrDefaultAsync(m => m.MeetingId == meeting.Id, cancellationToken);

        if (existing is null)
        {
            existing = new MeetingMinutes
            {
                MeetingId = meeting.Id,
                CreatedById = userId
            };
            _context.MeetingMinutes.Add(existing);
        }

        existing.RawNotes = request.RawNotes;
        existing.GeneratedContent = generated;
        existing.CreatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<MinutesDto>.Ok(MinutesDto.From(existing, meeting.Title));
    }

    public async Task<List<MinutesDto>> GetMinutesAsync(int? meetingId)
    {
        var query = _context.MeetingMinutes.Include(m => m.Meeting).AsQueryable();

        if (meetingId is int id)
        {
            query = query.Where(m => m.MeetingId == id);
        }

        var items = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

        return items.Select(m => MinutesDto.From(m, m.Meeting.Title)).ToList();
    }

    private async Task<string> BuildExplanationAsync(AiProposalDto proposal, CancellationToken cancellationToken)
    {
        var facts = new StringBuilder();
        facts.AppendLine($"Créneau demandé : {proposal.Date:dd/MM/yyyy} de {proposal.StartTime:HH\\:mm} à {proposal.EndTime:HH\\:mm}.");
        facts.AppendLine($"Salle proposée : {proposal.ProposedRoomName ?? "aucune salle libre"}.");
        facts.AppendLine($"Disponible : {(proposal.IsAvailable ? "oui" : "non")}.");

        foreach (var conflict in proposal.Conflicts)
        {
            facts.AppendLine($"Conflit : {conflict.Message}");
        }

        foreach (var alternative in proposal.Alternatives)
        {
            facts.AppendLine($"Alternative : {alternative.Date:dd/MM/yyyy} {alternative.StartTime:HH\\:mm}-{alternative.EndTime:HH\\:mm} en {alternative.RoomName}.");
        }

        try
        {
            return await _llmClient.CompleteTextAsync(AiPrompts.ExplanationSystemPrompt, facts.ToString(), cancellationToken);
        }
        catch
        {
            // Si le LLM échoue, on renvoie une explication construite par le backend.
            return proposal.IsAvailable
                ? $"Créneau disponible le {proposal.Date:dd/MM/yyyy} de {proposal.StartTime:HH\\:mm} à {proposal.EndTime:HH\\:mm}."
                : "Le créneau demandé n'est pas disponible. Consultez les créneaux alternatifs proposés.";
        }
    }
}
