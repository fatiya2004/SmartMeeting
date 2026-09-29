using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;
using SmartMeeting.Api.Services;

namespace SmartMeeting.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Liste utilisée pour choisir les participants d'une réunion.</summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
        => Ok(await _userService.GetAllAsync());

    [HttpPut("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> UpdateRole(int id, [FromQuery] UserRole role)
    {
        var result = await _userService.UpdateRoleAsync(id, role);
        return result.Success ? Ok(result.Data) : NotFound(new MessageResponse(result.Error!));
    }

    [HttpPut("{id:int}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> SetActive(int id, [FromQuery] bool isActive)
    {
        var result = await _userService.SetActiveAsync(id, isActive);
        return result.Success ? Ok(result.Data) : NotFound(new MessageResponse(result.Error!));
    }
}
