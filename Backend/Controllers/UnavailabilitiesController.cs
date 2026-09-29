using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Services;

namespace SmartMeeting.Api.Controllers;

[ApiController]
[Route("api/unavailabilities")]
[Authorize]
public class UnavailabilitiesController : ControllerBase
{
    private readonly IUnavailabilityService _service;

    public UnavailabilitiesController(IUnavailabilityService service)
    {
        _service = service;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<UnavailabilityDto>>> GetMine()
        => Ok(await _service.GetForUserAsync(this.Id()));

    [HttpGet]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<ActionResult<List<UnavailabilityDto>>> GetAll([FromQuery] DateOnly? date)
        => Ok(await _service.GetAllAsync(date));

    [HttpPost]
    public async Task<ActionResult<UnavailabilityDto>> Create([FromBody] UnavailabilityRequest request)
    {
        var result = await _service.CreateAsync(request, this.Id());
        return result.Success ? Ok(result.Data) : BadRequest(new MessageResponse(result.Error!));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MessageResponse>> Delete(int id)
    {
        var result = await _service.DeleteAsync(id, this.Id(), this.IsStaff());
        return result.Success
            ? Ok(new MessageResponse("Indisponibilité supprimée."))
            : BadRequest(new MessageResponse(result.Error!));
    }
}
