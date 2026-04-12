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
        var cities = await _cities.GetAllAsync(ct);

        return Ok(cities.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            center = new
            {
                lat = c.Center.Lat,
                lng = c.Center.Lng
            },
            zoom = c.DefaultZoom
        }));
    }
}