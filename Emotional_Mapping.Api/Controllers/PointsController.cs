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

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddEmotionalPointDto dto, CancellationToken ct)
        => Ok(new { id = await _service.AddAsync(dto, ct) });

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    } 
    
}