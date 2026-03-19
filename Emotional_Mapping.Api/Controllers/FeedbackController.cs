using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly FeedbackService _service;

    public FeedbackController(FeedbackService service)
    {
        _service = service;
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] FeedbackDto dto, CancellationToken ct)
    {
        await _service.AddAsync(dto, ct);
        return Ok();
    }
}