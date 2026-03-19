using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Services;
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
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] ReportDto dto, CancellationToken ct)
    {
        await _service.AddAsync(dto, ct);
        return Ok();
    }
}