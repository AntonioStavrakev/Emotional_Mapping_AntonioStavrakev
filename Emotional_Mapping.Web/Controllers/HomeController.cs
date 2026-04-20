using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emotional_Mapping.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Emotional_Mapping.Web.Models;
using Microsoft.Extensions.Logging;

namespace Emotional_Mapping.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // GET /
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(new HomeIndexViewModel
        {
            HeroEmotionKeys = await LoadHeroEmotionKeysAsync(ct)
        });
    }

    // GET /Privacy
    [HttpGet("/Privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult PaymentSuccess(string? sessionId)
    {
        ViewBag.SessionId = sessionId;
        return View();
    }

    [HttpGet]
    public IActionResult PaymentCancel()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private async Task<IReadOnlyList<string>> LoadHeroEmotionKeysAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api");
            using var response = await client.GetAsync("/api/stats/dashboard", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load homepage hero emotions. StatusCode={StatusCode}", response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var dashboard = await JsonSerializer.DeserializeAsync<AiUsageDashboardDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct);

            return (dashboard?.TopEmotions ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.Emotion) && x.Count > 0)
                .Take(7)
                .Select(x => x.Emotion)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load homepage hero emotions.");
            return [];
        }
    }
}
