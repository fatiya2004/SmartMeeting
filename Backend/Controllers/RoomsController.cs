using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Services;

namespace SmartMeeting.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll([FromQuery] bool onlyActive = false)
    {
        return Ok(await _roomService.GetAllAsync(onlyActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id)
    {
        var room = await _roomService.GetByIdAsync(id);
        return room is null ? NotFound(new MessageResponse("Salle introuvable.")) : Ok(room);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] RoomRequest request)
    {
        var result = await _roomService.CreateAsync(request);

        if (!result.Success)
        {
            return BadRequest(new MessageResponse(result.Error!));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] RoomRequest request)
    {
        var result = await _roomService.UpdateAsync(id, request);

        if (!result.Success)
        {
            return BadRequest(new MessageResponse(result.Error!));
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MessageResponse>> Delete(int id)
    {
        var result = await _roomService.DeleteAsync(id);

        if (!result.Success)
        {
            return NotFound(new MessageResponse(result.Error!));
        }

        return Ok(new MessageResponse(result.Data
            ? "Salle supprimée."
            : "Salle désactivée car elle est utilisée par des réunions existantes."));
    }
}
