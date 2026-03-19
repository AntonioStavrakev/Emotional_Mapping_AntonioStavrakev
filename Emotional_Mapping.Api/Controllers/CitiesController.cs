using Emotional_Mapping.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/cities")]
public class CitiesController : ControllerBase
{
    private readonly ICityRepository _cities;

    public CitiesController(ICityRepository cities)
    {
        _cities = cities;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await _cities.GetAllAsync(ct));
    }
}