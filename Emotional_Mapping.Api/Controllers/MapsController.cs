using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Application.Services;
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

    public MapsController(
        MapGenerationService service,
        IMapRepository maps,
        ICurrentUser currentUser)
    {
        _service = service;
        _maps = maps;
        _currentUser = currentUser;
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

        return Ok(new
        {
            id = map.Id,
            title = map.Title,
            dominantEmotion = map.DominantEmotion.ToString(),
            confidence = map.Confidence,
            summary = map.Summary,
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
        });
    }

    // User's own maps
    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var maps = await _maps.GetUserMapsAsync(userId, ct);

        return Ok(maps.Select(m => new
        {
            id = m.Id,
            title = m.Title,
            dominantEmotion = m.DominantEmotion.ToString(),
            confidence = m.Confidence,
            summary = m.Summary,
            generatedAtUtc = m.GeneratedAtUtc
        }));
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

        return Ok(new { publicSlug = slug });
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
            publicUrl = $"/api/maps/public/{slug}"
        });
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<IActionResult> GetPublicBySlug(string slug, CancellationToken ct)
    {
        var map = await _maps.GetBySlugAsync(slug, ct);
        if (map == null) return NotFound();
        if (map.Visibility != MapVisibility.Public) return NotFound();

        return Ok(map);
    }
}
