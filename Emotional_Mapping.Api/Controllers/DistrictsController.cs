using Emotional_Mapping.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/districts")]
public class DistrictsController : ControllerBase
{
    private readonly IDistrictRepository _districts;

    public DistrictsController(IDistrictRepository districts)
    {
        _districts = districts;
    }

    [HttpGet("by-city/{cityId:guid}")]
    public async Task<IActionResult> GetByCity(Guid cityId, CancellationToken ct)
    {
        var result = await _districts.GetByCityAsync(cityId, ct);

        return Ok(result.Select(x => new
        {
            x.Id,
            x.Name,
            x.CityId
        }));
    }
}