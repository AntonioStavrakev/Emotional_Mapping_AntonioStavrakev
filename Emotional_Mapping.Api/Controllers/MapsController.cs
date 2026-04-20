using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Application.Services;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/maps")]
public class MapsController : ControllerBase
{
    private readonly MapGenerationService _service;
    private readonly IMapRepository _maps;
    private readonly ICurrentUser _currentUser;
    private readonly IAiEmotionService _aiEmotionService;

    public MapsController(
        MapGenerationService service,
        IMapRepository maps,
        ICurrentUser currentUser,
        IAiEmotionService aiEmotionService)
    {
        _service = service;
        _maps = maps;
        _currentUser = currentUser;
        _aiEmotionService = aiEmotionService;
    }

    // Guest
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublic([FromQuery] Guid cityId, CancellationToken ct)
    {
        var maps = await _maps.GetPublicMapsAsync(cityId, ct);

        return Ok(maps.Select(map => new
        {
            id = map.Id,
            title = map.Title,
            dominantEmotion = map.DominantEmotion.ToString(),
            confidence = map.Confidence,
            summary = map.Summary,
            publicSlug = map.PublicSlug,
            generatedAtUtc = map.GeneratedAtUtc,
            recommendations = map.Recommendations.Select(r => new
            {
                id = r.Id,
                emotion = r.Emotion.ToString(),
                score = r.Score,
                reason = r.Reason,
                place = r.Place != null ? new
                {
                    id = r.Place.Id,
                    name = r.Place.Name,
                    lat = r.Place.Location.Lat,
                    lng = r.Place.Location.Lng,
                    description = r.Place.Description,
                    type = r.Place.Type.ToString()
                } : null
            })
        }));
    }

    // Get single map by ID
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var map = await _maps.GetGeneratedMapAsync(id, ct);
        if (map == null) return NotFound();

        return Ok(await BuildMapResponseAsync(map, ct));
    }

    // User's own maps
    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var maps = await _maps.GetUserMapsAsync(userId, ct);
        var targetLanguage = ResolveRequestLanguage();

        var result = await Task.WhenAll(maps.Select(async m => new
        {
            id = m.Id,
            title = m.Title,
            dominantEmotion = m.DominantEmotion.ToString(),
            confidence = m.Confidence,
            summary = await LocalizeSummaryAsync(m.Summary, m.MapRequest?.Language, targetLanguage, ct),
            generatedAtUtc = m.GeneratedAtUtc
        }));

        return Ok(result);
    }

    // Guest/User
    [AllowAnonymous]
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateMapRequestDto dto, CancellationToken ct)
        => Ok(await _service.GenerateAsync(dto, ct));

    [Authorize]
    [HttpGet("quota")]
    public async Task<IActionResult> GetQuota(CancellationToken ct)
        => Ok(await _service.GetDailyQuotaAsync(ct));

    // SuperUser + Admin
    [Authorize(Roles = "SuperUser,Admin")]
    [HttpPost("generate-premium")]
    public async Task<IActionResult> GeneratePremium([FromBody] GenerateMapRequestDto dto, CancellationToken ct)
        => Ok(await _service.GenerateAsync(dto, ct));

    // Make map public
    [Authorize]
    [HttpPost("{id:guid}/make-public")]
    public async Task<IActionResult> MakePublic(Guid id, CancellationToken ct)
    {
        var map = await _maps.GetGeneratedMapAsync(id, ct);
        if (map == null) return NotFound();

        if (map.MapRequest.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
            return Forbid();

        var slug = map.PublicSlug;
        if (string.IsNullOrEmpty(slug))
        {
            slug = Guid.NewGuid().ToString("N");
            map.Publish(slug);
            await _maps.SaveChangesAsync(ct);
        }

        return Ok(new
        {
            publicSlug = slug,
            publicUrl = $"/Map/Public/{slug}"
        });
    }

    // Publish share link
    [Authorize]
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var map = await _maps.GetGeneratedMapAsync(id, ct);
        if (map == null) return NotFound();

        if (map.MapRequest.UserId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
            return Forbid();

        var slug = Guid.NewGuid().ToString("N");
        map.Publish(slug);
        await _maps.SaveChangesAsync(ct);

        return Ok(new
        {
            slug,
            publicUrl = $"/Map/Public/{slug}"
        });
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<IActionResult> GetPublicBySlug(string slug, CancellationToken ct)
    {
        var map = await _maps.GetBySlugAsync(slug, ct);
        if (map == null) return NotFound();
        if (map.Visibility != MapVisibility.Public) return NotFound();

        return Ok(await BuildMapResponseAsync(map, ct));
    }

    private string ResolveRequestLanguage()
    {
        var acceptLanguage = Request.Headers.AcceptLanguage.ToString();
        return acceptLanguage.Contains("en", StringComparison.OrdinalIgnoreCase) ? "en" : "bg";
    }

    private async Task<string?> LocalizeSummaryAsync(
        string? summary,
        string? sourceLanguage,
        string targetLanguage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return summary;

        var normalizedSourceLanguage = string.Equals(sourceLanguage, "en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "bg";

        if (normalizedSourceLanguage == targetLanguage)
            return summary;

        if (targetLanguage == "en" && !ContainsCyrillic(summary))
            return summary;

        return await _aiEmotionService.TranslateTextAsync(summary, targetLanguage, ct);
    }

    private static bool ContainsCyrillic(string text)
    {
        foreach (var ch in text)
        {
            if ((ch >= 'А' && ch <= 'я') || ch == 'Ѝ' || ch == 'ѝ')
                return true;
        }

        return false;
    }

    private async Task<object> BuildMapResponseAsync(GeneratedMap map, CancellationToken ct)
    {
        var targetLanguage = ResolveRequestLanguage();

        return new
        {
            id = map.Id,
            title = map.Title,
            dominantEmotion = map.DominantEmotion.ToString(),
            confidence = map.Confidence,
            summary = await LocalizeSummaryAsync(map.Summary, map.MapRequest?.Language, targetLanguage, ct),
            publicSlug = map.PublicSlug,
            visibility = map.Visibility.ToString(),
            generatedAtUtc = map.GeneratedAtUtc,
            recommendations = map.Recommendations.Select(r => new
            {
                id = r.Id,
                emotion = r.Emotion.ToString(),
                score = r.Score,
                reason = r.Reason,
                place = r.Place != null ? new
                {
                    id = r.Place.Id,
                    name = r.Place.Name,
                    lat = r.Place.Location.Lat,
                    lng = r.Place.Location.Lng,
                    description = r.Place.Description,
                    type = r.Place.Type.ToString()
                } : null
            })
        };
    }
}
