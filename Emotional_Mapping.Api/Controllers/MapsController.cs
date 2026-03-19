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
        => Ok(await _maps.GetPublicMapsAsync(cityId, ct));

    // User
    [Authorize]
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateMapRequestDto dto, CancellationToken ct)
        => Ok(await _service.GenerateAsync(dto, ct));

    // SuperUser + Admin
    [Authorize(Roles = "SuperUser,Admin")]
    [HttpPost("generate-premium")]
    public async Task<IActionResult> GeneratePremium([FromBody] GenerateMapRequestDto dto, CancellationToken ct)
        => Ok(await _service.GenerateAsync(dto, ct));

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
    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<IActionResult> GetPublic(string token, CancellationToken ct)
    {
        var map = await _maps.GetBySlugAsync(token, ct);

        if (map == null)
            return NotFound();

        return Ok(map);
    }
}