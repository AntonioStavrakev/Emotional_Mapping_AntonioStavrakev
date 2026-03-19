using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/places")]
public class PlacesController : ControllerBase
{
    private readonly IPlaceRepository _places;

    public PlacesController(IPlaceRepository places)
    {
        _places = places;
    }

    [HttpGet("similar/{placeId:guid}")]
    public async Task<IActionResult> Similar(Guid placeId, CancellationToken ct)
    {
        var place = await _places.GetAsync(placeId, ct);
        if (place == null) return NotFound();

        var candidates = await _places.GetByCityAsync(place.CityId, place.DistrictId, place.Type, ct);

        var result = candidates
            .Where(x => x.Id != place.Id)
            .Take(5)
            .Select(x => new SimilarPlaceDto
            {
                PlaceId = x.Id,
                Name = x.Name,
                Reason = $"Подобен тип място: {x.Type}"
            });

        return Ok(result);
    }
}