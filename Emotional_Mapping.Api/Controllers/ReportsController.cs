using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Services;
using Emotional_Mapping.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _service;

    public ReportsController(ReportService service)
    {
        _service = service;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var reports = await _service.GetActiveAsync(ct);
        return Ok(reports.Select(r => new
        {
            r.Id,
            r.ReporterUserId,
            r.EmotionalPointId,
            r.PlaceId,
            r.Reason,
            status = r.Status.ToString(),
            r.ModeratorNote,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            targetType = r.EmotionalPointId.HasValue ? "Point" : "Place",
            pointTitle = r.EmotionalPoint?.Title,
            pointNote = r.EmotionalPoint?.Note,
            pointEmotion = r.EmotionalPoint?.Emotion.ToString(),
            pointIsApproved = r.EmotionalPoint?.IsApproved,
            placeName = r.Place?.Name,
            placeAddress = r.Place?.Address
        }));
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] ReportDto dto, CancellationToken ct)
    {
        await _service.AddAsync(dto, ct);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> MarkInReview(Guid id, CancellationToken ct)
    {
        await _service.SetStatusAsync(id, ReportStatus.InReview, ct);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        await _service.SetStatusAsync(id, ReportStatus.Resolved, ct);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        await _service.SetStatusAsync(id, ReportStatus.Rejected, ct);
        return Ok();
    }
}
