using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Emotional_Mapping.Application.Places;
using Emotional_Mapping.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure.Places;

public class FoursquarePlacesDiscoveryService : IExternalPlaceProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly FoursquarePlacesOptions _options;
    private readonly ILogger<FoursquarePlacesDiscoveryService> _logger;

    public FoursquarePlacesDiscoveryService(
        IHttpClientFactory httpFactory,
        IOptions<FoursquarePlacesOptions> options,
        ILogger<FoursquarePlacesDiscoveryService> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "foursquare";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<List<DiscoveredPlaceCandidate>> SearchAsync(PlaceDiscoveryRequest request, CancellationToken ct)
    {
        if (!IsConfigured)
            return new List<DiscoveredPlaceCandidate>();

        try
        {
            var query = Uri.EscapeDataString(BuildQuery(request));
            var ll = $"{request.CenterLat.ToString(CultureInfo.InvariantCulture)},{request.CenterLng.ToString(CultureInfo.InvariantCulture)}";
            var radius = Math.Clamp(request.RadiusMeters ?? 3000, 500, 10000);
            var limit = Math.Clamp(_options.MaxResults, 1, 20);

            var http = _httpFactory.CreateClient("foursquare-places");
            using var response = await http.GetAsync($"search?ll={ll}&radius={radius}&limit={limit}&query={query}&sort=DISTANCE", ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<FoursquareSearchResponse>(cancellationToken: ct);
            var results = payload?.Results ?? new List<FoursquarePlaceItem>();

            return results
                .Where(x => x.Geocodes?.Main is not null && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => new DiscoveredPlaceCandidate
                {
                    ExternalId = $"foursquare:{x.FsqId}",
                    Name = x.Name!.Trim(),
                    Type = MapFoursquareType(x.Categories, request.PlaceTypeHint),
                    Lat = x.Geocodes!.Main!.Latitude,
                    Lng = x.Geocodes.Main.Longitude,
                    Address = x.Location?.FormattedAddress,
                    Description = BuildDescription(x),
                    Source = "foursquare"
                })
                .GroupBy(x => x.Name.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Foursquare discovery failed for {CityName}.", request.CityName);
            return new List<DiscoveredPlaceCandidate>();
        }
    }

    private static string BuildQuery(PlaceDiscoveryRequest request)
    {
        var text = (request.QueryText ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(text))
            return $"{text} {request.CityName}";

        return request.PlaceTypeHint?.ToString() ?? request.CityName;
    }

    private static PlaceType MapFoursquareType(List<FoursquareCategory>? categories, PlaceType? fallback)
    {
        var names = (categories ?? new List<FoursquareCategory>())
            .Select(x => x.Name ?? string.Empty)
            .ToList();

        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "nightclub", "night club"))) return PlaceType.Nightlife;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "bar", "pub", "cocktail"))) return PlaceType.Bar;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "cafe", "coffee"))) return PlaceType.Cafe;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "restaurant", "food"))) return PlaceType.Restaurant;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "park", "garden"))) return PlaceType.Park;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "museum"))) return PlaceType.Museum;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "gallery"))) return PlaceType.Gallery;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "gym", "fitness"))) return PlaceType.Gym;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "stadium", "sports"))) return PlaceType.Sport;
        if (names.Any(x => ContainsAny(x.ToLowerInvariant(), "landmark", "plaza", "monument", "historic", "attraction"))) return PlaceType.Landmark;

        return fallback ?? PlaceType.Other;
    }

    private static string? BuildDescription(FoursquarePlaceItem place)
    {
        var category = place.Categories?.FirstOrDefault()?.Name;
        if (string.IsNullOrWhiteSpace(category))
            return place.Location?.FormattedAddress;
        if (string.IsNullOrWhiteSpace(place.Location?.FormattedAddress))
            return category;
        return $"{category}, {place.Location.FormattedAddress}";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    private sealed class FoursquareSearchResponse
    {
        [JsonPropertyName("results")]
        public List<FoursquarePlaceItem>? Results { get; set; }
    }

    private sealed class FoursquarePlaceItem
    {
        [JsonPropertyName("fsq_id")]
        public string FsqId { get; set; } = "";

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("categories")]
        public List<FoursquareCategory>? Categories { get; set; }

        [JsonPropertyName("geocodes")]
        public FoursquareGeocodes? Geocodes { get; set; }

        [JsonPropertyName("location")]
        public FoursquareLocation? Location { get; set; }
    }

    private sealed class FoursquareCategory
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class FoursquareGeocodes
    {
        [JsonPropertyName("main")]
        public FoursquareLatLng? Main { get; set; }
    }

    private sealed class FoursquareLatLng
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class FoursquareLocation
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }
    }
}
