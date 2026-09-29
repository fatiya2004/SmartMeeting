using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeeting.Api.AI;
using SmartMeeting.Api.DTOs;

namespace SmartMeeting.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _assistant;

    public AiController(IAiAssistantService assistant)
    {
        _assistant = assistant;
    }

    [HttpGet("status")]
    public ActionResult<object> Status() => Ok(new { configured = _assistant.IsConfigured });

    /// <summary>Analyse une demande en langage naturel et vérifie réellement les disponibilités.</summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<AiProposalDto>> Analyze([FromBody] NaturalLanguageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assistant.AnalyzeAsync(request.Text, this.Id(), cancellationToken);
        return result.Success ? Ok(result.Data) : BadRequest(new MessageResponse(result.Error!));
    }

    [HttpPost("minutes")]
    public async Task<ActionResult<MinutesDto>> GenerateMinutes([FromBody] MinutesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assistant.GenerateMinutesAsync(request, this.Id(), cancellationToken);
        return result.Success ? Ok(result.Data) : BadRequest(new MessageResponse(result.Error!));
    }

    [HttpGet("minutes")]
    public async Task<ActionResult<List<MinutesDto>>> GetMinutes([FromQuery] int? meetingId)
        => Ok(await _assistant.GetMinutesAsync(meetingId));
}
