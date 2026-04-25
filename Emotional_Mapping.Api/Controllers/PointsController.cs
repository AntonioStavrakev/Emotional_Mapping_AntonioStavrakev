using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Application.Services;
using Emotional_Mapping.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;


[ApiController]
[Route("api/points")]
public class PointsController : ControllerBase
{
    private readonly IEmotionalPointRepository _repo;
    private readonly EmotionalPointsService _service;

    public PointsController(IEmotionalPointRepository repo, EmotionalPointsService service)
    {
        _repo = repo;
        _service = service;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid cityId, [FromQuery] EmotionType? emotion, CancellationToken ct)
        => Ok(await _repo.GetByCityAsync(cityId, emotion, ct));

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var points = await _repo.GetPendingAsync(ct);
        return Ok(points.Select(p => new
        {
            p.Id,
            p.UserId,
            p.CityId,
            cityName = p.City.Name,
            p.PlaceId,
            placeName = p.Place != null ? p.Place.Name : null,
            p.DistrictId,
            districtName = p.District != null ? p.District.Name : null,
            emotion = p.Emotion.ToString(),
            p.Intensity,
            p.Title,
            p.Note,
            p.TimeOfDay,
            p.IsAnonymous,
            p.IsApproved,
            p.CreatedAtUtc
        }));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddEmotionalPointDto dto, CancellationToken ct)
        => Ok(new { id = await _service.AddAsync(dto, ct) });

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await _service.ApproveAsync(id, ct);
        return Ok();
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    } 
    
}
