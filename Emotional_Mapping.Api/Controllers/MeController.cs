using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IMapRepository _maps;

    public MeController(ICurrentUser currentUser, IMapRepository maps)
    {
        _currentUser = currentUser;
        _maps = maps;
    }

    [HttpGet("maps")]
    public async Task<IActionResult> MyMaps(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Unauthorized();

        var list = await _maps.GetUserMapsAsync(_currentUser.UserId!, ct);

        var result = list.Select(m => new MyMapDto
        {
            Id = m.Id,
            Title = m.Title,
            DominantEmotion = m.DominantEmotion,
            Confidence = m.Confidence,
            GeneratedAtUtc = m.GeneratedAtUtc,
            Summary = m.Summary,
            HeatmapJson = m.HeatmapJson,
            RecommendationsCount = m.Recommendations.Count
        });

        return Ok(result);
    }
}