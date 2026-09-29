using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;
using SmartMeeting.Api.Services;

namespace SmartMeeting.Api.Controllers;

[ApiController]
[Route("api/meetings")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;

    public MeetingsController(IMeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<ActionResult<List<MeetingDto>>> GetAll([FromQuery] MeetingFilter filter)
        => Ok(await _meetingService.GetAllAsync(filter));

    [HttpGet("mine")]
    public async Task<ActionResult<List<MeetingDto>>> GetMine()
        => Ok(await _meetingService.GetForUserAsync(this.Id()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MeetingDto>> GetById(int id)
    {
        var meeting = await _meetingService.GetByIdAsync(id);
        return meeting is null ? NotFound(new MessageResponse("Réunion introuvable.")) : Ok(meeting);
    }

    [HttpPost]
    public async Task<ActionResult<MeetingDto>> Create([FromBody] CreateMeetingRequest request)
    {
        var result = await _meetingService.CreateAsync(request, this.Id());

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : Conflict(new MessageResponse(result.Error!));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MeetingDto>> Update(int id, [FromBody] UpdateMeetingRequest request)
    {
        var result = await _meetingService.UpdateAsync(id, request, this.Id(), this.IsStaff());

        return result.Success ? Ok(result.Data) : Conflict(new MessageResponse(result.Error!));
    }

    [HttpPost("check-availability")]
    public async Task<ActionResult<AvailabilityResultDto>> CheckAvailability(
        [FromBody] AvailabilityRequest request,
        [FromQuery] MeetingPriority priority = MeetingPriority.Normal)
        => Ok(await _meetingService.CheckAvailabilityAsync(request, priority));

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<ActionResult<MeetingDto>> Approve(int id)
    {
        var result = await _meetingService.ApproveAsync(id);
        return result.Success ? Ok(result.Data) : Conflict(new MessageResponse(result.Error!));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<ActionResult<MeetingDto>> Reject(int id, [FromBody] DecisionRequest request)
    {
        var result = await _meetingService.RejectAsync(id, request.Reason ?? string.Empty);
        return result.Success ? Ok(result.Data) : BadRequest(new MessageResponse(result.Error!));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<MeetingDto>> Cancel(int id)
    {
        var result = await _meetingService.CancelAsync(id, this.Id(), this.IsStaff());
        return result.Success ? Ok(result.Data) : BadRequest(new MessageResponse(result.Error!));
    }

    [HttpPost("{id:int}/respond")]
    public async Task<ActionResult<MeetingDto>> Respond(int id, [FromBody] RespondInvitationRequest request)
    {
        var result = await _meetingService.RespondAsync(id, this.Id(), request.Accept);
        return result.Success ? Ok(result.Data) : BadRequest(new MessageResponse(result.Error!));
    }

    /// <summary>Réunion urgente : déplace une réunion normale puis valide l'urgente.</summary>
    [HttpPost("{urgentId:int}/preempt/{normalId:int}")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<ActionResult<MeetingDto>> Preempt(int urgentId, int normalId)
    {
        var result = await _meetingService.PreemptAsync(urgentId, normalId);
        return result.Success ? Ok(result.Data) : Conflict(new MessageResponse(result.Error!));
    }
}
