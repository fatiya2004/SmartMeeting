using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Services;

namespace SmartMeeting.Api.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> Dashboard()
        => Ok(await _statisticsService.GetDashboardAsync(this.Id(), this.IsStaff()));

    [HttpGet("global")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<ActionResult<GlobalStatsDto>> Global()
        => Ok(await _statisticsService.GetGlobalAsync());
}
