using Emotional_Mapping.Application.AI;
using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiEmotionService _ai;

    public AiController(IAiEmotionService ai)
    {
        _ai = ai;
    }

    [Authorize]
    [HttpPost("predict-emotion")]
    public async Task<IActionResult> PredictEmotion([FromBody] EmotionPredictionDto dto, CancellationToken ct)
    {
        var result = await _ai.AnalyzeAsync(new AiAnalysisInput
        {
            QueryText = dto.QueryText,
            CityName = "Unknown"
        }, ct);

        return Ok(new EmotionPredictionDto
        {
            QueryText = dto.QueryText,
            PredictedEmotion = result.FinalEmotion.ToString(),
            Confidence = result.Confidence
        });
    }
}