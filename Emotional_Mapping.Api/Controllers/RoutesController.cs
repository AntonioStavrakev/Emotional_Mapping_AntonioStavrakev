using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly ISavedRouteRepository _routes;
    private readonly ICurrentUser _currentUser;

    public RoutesController(ISavedRouteRepository routes, ICurrentUser currentUser)
    {
        _routes = routes;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> SaveRoute([FromBody] SavedRouteDto dto, CancellationToken ct)
    {
        var route = new SavedRoute(
            _currentUser.UserId!,
            dto.Name,
            dto.RouteJson
        );

        await _routes.AddAsync(route, ct);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> MyRoutes(CancellationToken ct)
    {
        var list = await _routes.GetByUserAsync(_currentUser.UserId!, ct);

        return Ok(list.Select(x => new SavedRouteDto
        {
            Id = x.Id,
            Name = x.Name,
            RouteJson = x.RouteJson
        }));
    }
}