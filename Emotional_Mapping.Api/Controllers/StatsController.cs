using Emotional_Mapping.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;


[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly StatsService _service;

    public StatsController(StatsService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid cityId, CancellationToken ct)
        => Ok(await _service.GetAsync(cityId, ct));
    
    [AllowAnonymous]
    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline([FromQuery] Guid cityId, CancellationToken ct)
        => Ok(await _service.GetTimelineAsync(cityId, ct));
    
    [Authorize(Roles = "Admin")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        return Ok(await _service.GetAiDashboardAsync(ct));
    }
    
    [AllowAnonymous]
    [HttpGet("district-scores")]
    public async Task<IActionResult> DistrictScores(Guid cityId, CancellationToken ct)
    {
        return Ok(await _service.GetDistrictScoresAsync(cityId, ct));
    }
}