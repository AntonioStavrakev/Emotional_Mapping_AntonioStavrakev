using Emotional_Mapping.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

public class MapController : Controller
{
    private readonly HttpClient _http;

    public MapController(IHttpClientFactory httpFactory)
    {
        _http = httpFactory.CreateClient("api");
    }

    [HttpGet]
    public IActionResult Generate()
    { 
        return View(new GenerateMapRequestDto());
    }

    [HttpPost]
    public async Task<IActionResult> Generate(GenerateMapRequestDto dto, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync("/api/maps/generate", dto, ct);
        res.EnsureSuccessStatusCode();

        var data = await res.Content.ReadFromJsonAsync<GenerateMapResultDto>(cancellationToken: ct);
        return View("Map", data);
    }
}