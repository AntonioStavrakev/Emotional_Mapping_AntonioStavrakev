using Emotional_Mapping.Application.Services;
using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;


[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly StatsService _service;
    private readonly UserManager<ApplicationUser> _users;

    public StatsController(StatsService service, UserManager<ApplicationUser> users)
    {
        _service = service;
        _users = users;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid cityId, CancellationToken ct)
        => Ok(await _service.GetAsync(cityId, ct));

    [AllowAnonymous]
    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline([FromQuery] Guid cityId, CancellationToken ct)
        => Ok(await _service.GetTimelineAsync(cityId, ct));

    [AllowAnonymous]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var data = await _service.GetAiDashboardAsync(ct);

        // Enrich TopUsers with email addresses
        foreach (var userStat in data.TopUsers)
        {
            if (!string.IsNullOrWhiteSpace(userStat.UserId))
            {
                var appUser = await _users.FindByIdAsync(userStat.UserId);
                if (appUser != null)
                    userStat.UserEmail = appUser.Email;
            }
        }

        return Ok(data);
    }

    [AllowAnonymous]
    [HttpGet("district-scores")]
    public async Task<IActionResult> DistrictScores(Guid cityId, CancellationToken ct)
    {
        return Ok(await _service.GetDistrictScoresAsync(cityId, ct));
    }
}