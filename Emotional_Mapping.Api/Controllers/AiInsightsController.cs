using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/ai-insights")]
public class AiInsightsController : ControllerBase
{
    private readonly IPlaceRepository _places;

    public AiInsightsController(IPlaceRepository places)
    {
        _places = places;
    }

    [HttpGet("clusters")]
    public async Task<IActionResult> Clusters([FromQuery] Guid cityId, CancellationToken ct)
    {
        var places = await _places.GetByCityAsync(cityId, null, null, ct);

        var result = new List<MoodClusterDto>
        {
            new MoodClusterDto
            {
                Emotion = "Calm",
                Places = places.Where(p => p.Type.ToString().Contains("Park") || p.Type.ToString().Contains("Garden"))
                    .Take(5)
                    .Select(p => p.Name)
                    .ToList()
            },
            new MoodClusterDto
            {
                Emotion = "Social",
                Places = places.Where(p => p.Type.ToString().Contains("Cafe") || p.Type.ToString().Contains("Restaurant"))
                    .Take(5)
                    .Select(p => p.Name)
                    .ToList()
            },
            new MoodClusterDto
            {
                Emotion = "Inspiration",
                Places = places.Where(p => p.Type.ToString().Contains("Museum") || p.Type.ToString().Contains("Gallery"))
                    .Take(5)
                    .Select(p => p.Name)
                    .ToList()
            }
        };

        return Ok(result);
    }
}