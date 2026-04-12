using System.Globalization;
using System.Text;
using System.Text.Json;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Places;
using Emotional_Mapping.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure.Places;

public class OpenStreetMapPlaceDiscoveryService : IExternalPlaceProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OpenStreetMapOptions _options;
    private readonly ILogger<OpenStreetMapPlaceDiscoveryService> _logger;

    public OpenStreetMapPlaceDiscoveryService(
        IHttpClientFactory httpFactory,
        IOptions<OpenStreetMapOptions> options,
        ILogger<OpenStreetMapPlaceDiscoveryService> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "osm";
    public bool IsConfigured => _options.Enabled;

    public async Task<List<DiscoveredPlaceCandidate>> SearchAsync(PlaceDiscoveryRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return new List<DiscoveredPlaceCandidate>();

        try
        {
            var radius = Math.Clamp(request.RadiusMeters ?? _options.DefaultRadiusMeters, 500, 10000);
            var filters = BuildFilters(request);
            if (filters.Count == 0)
                return new List<DiscoveredPlaceCandidate>();

            var overpassQuery = BuildOverpassQuery(request.CenterLat, request.CenterLng, radius, filters);
            var http = _httpFactory.CreateClient("osm-overpass");

            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", overpassQuery)
            });

            using var response = await http.PostAsync("interpreter", content, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var discovered = new List<DiscoveredPlaceCandidate>();
            if (!json.RootElement.TryGetProperty("elements", out var elements) || elements.ValueKind != JsonValueKind.Array)
                return discovered;

            foreach (var element in elements.EnumerateArray())
            {
                if (!TryReadCandidate(element, request, out var candidate))
                    continue;

                discovered.Add(candidate);
            }

            return discovered
                .GroupBy(x => NormalizeName(x.Name))
                .Select(g => g
                    .OrderByDescending(x => ScoreCandidate(x, request))
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .First())
                .OrderByDescending(x => ScoreCandidate(x, request))
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(_options.MaxResults, 1))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenStreetMap discovery failed for {CityName}. Falling back to cache only.", request.CityName);
            return new List<DiscoveredPlaceCandidate>();
        }
    }

    private static string BuildOverpassQuery(double lat, double lng, int radius, List<OverpassFilter> filters)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[out:json][timeout:20];");
        sb.AppendLine("(");

        foreach (var filter in filters)
        {
            var key = EscapeValue(filter.Key);
            var value = EscapeValue(filter.Value);
            sb.AppendLine($"  node(around:{radius},{lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)})[\"{key}\"=\"{value}\"];");
            sb.AppendLine($"  way(around:{radius},{lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)})[\"{key}\"=\"{value}\"];");
            sb.AppendLine($"  relation(around:{radius},{lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)})[\"{key}\"=\"{value}\"];");
        }

        sb.AppendLine(");");
        sb.AppendLine("out center tags;");
        return sb.ToString();
    }

    private static bool TryReadCandidate(JsonElement element, PlaceDiscoveryRequest request, out DiscoveredPlaceCandidate candidate)
    {
        candidate = new DiscoveredPlaceCandidate();

        if (!element.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
            return false;

        var name = TryGetString(tags, "name");
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!TryReadCoordinates(element, out var lat, out var lng))
            return false;

        var type = MapType(tags, request.PlaceTypeHint);
        var externalId = BuildExternalId(element);
        var address = BuildAddress(tags, request.CityName);
        var description = BuildDescription(name, type, address, request.Language);

        candidate = new DiscoveredPlaceCandidate
        {
            ExternalId = externalId,
            Name = name.Trim(),
            Type = type,
            Lat = lat,
            Lng = lng,
            Address = address,
            Description = description,
            Source = "osm-overpass"
        };

        return true;
    }

    private static bool TryReadCoordinates(JsonElement element, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;

        if (element.TryGetProperty("lat", out var latValue) &&
            element.TryGetProperty("lon", out var lngValue) &&
            latValue.TryGetDouble(out lat) &&
            lngValue.TryGetDouble(out lng))
        {
            return true;
        }

        if (element.TryGetProperty("center", out var center) &&
            center.ValueKind == JsonValueKind.Object &&
            center.TryGetProperty("lat", out var centerLat) &&
            center.TryGetProperty("lon", out var centerLng) &&
            centerLat.TryGetDouble(out lat) &&
            centerLng.TryGetDouble(out lng))
        {
            return true;
        }

        return false;
    }

    private static string BuildExternalId(JsonElement element)
    {
        var kind = element.TryGetProperty("type", out var type) ? type.GetString() : "unknown";
        var id = element.TryGetProperty("id", out var rawId) ? rawId.ToString() : Guid.NewGuid().ToString("N");
        return $"{kind}:{id}";
    }

    private static PlaceType MapType(JsonElement tags, PlaceType? requestedType)
    {
        var amenity = TryGetString(tags, "amenity");
        var leisure = TryGetString(tags, "leisure");
        var tourism = TryGetString(tags, "tourism");
        var historic = TryGetString(tags, "historic");
        var shop = TryGetString(tags, "shop");
        var natural = TryGetString(tags, "natural");

        return amenity switch
        {
            "nightclub" => PlaceType.Nightlife,
            "bar" => PlaceType.Bar,
            "pub" => PlaceType.Bar,
            "cafe" => PlaceType.Cafe,
            "restaurant" => PlaceType.Restaurant,
            "fast_food" => PlaceType.Restaurant,
            "theatre" => PlaceType.Theater,
            "cinema" => PlaceType.Cinema,
            "library" => PlaceType.Library,
            "university" => PlaceType.University,
            "school" => PlaceType.School,
            "hospital" => PlaceType.Hospital,
            "pharmacy" => PlaceType.Pharmacy,
            _ => leisure switch
            {
                "park" => PlaceType.Park,
                "garden" => PlaceType.Garden,
                "sports_centre" => PlaceType.Sport,
                "fitness_centre" => PlaceType.Gym,
                "stadium" => PlaceType.Stadium,
                "playground" => PlaceType.Playground,
                _ => tourism switch
                {
                    "museum" => PlaceType.Museum,
                    "gallery" => PlaceType.Gallery,
                    "viewpoint" => PlaceType.Viewpoint,
                    "attraction" => historic is not null ? PlaceType.HistoricSite : PlaceType.Landmark,
                    _ => historic is not null ? PlaceType.HistoricSite :
                         shop is not null ? PlaceType.Shopping :
                         natural == "beach" ? PlaceType.Beach :
                         natural == "wood" ? PlaceType.Forest :
                         requestedType ?? PlaceType.Other
                }
            }
        };
    }

    private static string? BuildAddress(JsonElement tags, string cityName)
    {
        var parts = new List<string>();
        var houseNumber = TryGetString(tags, "addr:housenumber");
        var street = TryGetString(tags, "addr:street");
        var suburb = TryGetString(tags, "addr:suburb");
        var city = TryGetString(tags, "addr:city") ?? cityName;

        if (!string.IsNullOrWhiteSpace(street))
        {
            parts.Add(string.IsNullOrWhiteSpace(houseNumber) ? street.Trim() : $"{street.Trim()} {houseNumber!.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(suburb))
            parts.Add(suburb.Trim());
        if (!string.IsNullOrWhiteSpace(city))
            parts.Add(city.Trim());

        if (parts.Count == 0)
            return null;

        return string.Join(", ", parts.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string? BuildDescription(string name, PlaceType type, string? address, string language)
    {
        var typeLabel = language.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? type.ToString()
            : type switch
            {
                PlaceType.Nightlife => "нощен живот",
                PlaceType.Bar => "бар",
                PlaceType.Cafe => "кафене",
                PlaceType.Restaurant => "ресторант",
                PlaceType.Park => "парк",
                PlaceType.CulturalSite => "културно място",
                PlaceType.Museum => "музей",
                PlaceType.Gallery => "галерия",
                PlaceType.Sport => "спортно място",
                PlaceType.Viewpoint => "гледка",
                _ => "място"
            };

        if (string.IsNullOrWhiteSpace(address))
        {
            return language.Equals("en", StringComparison.OrdinalIgnoreCase)
                ? $"{name} is a discovered {typeLabel.ToLowerInvariant()} location from OpenStreetMap."
                : $"{name} е открита {typeLabel} локация от OpenStreetMap.";
        }

        return language.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? $"{name} is a discovered {typeLabel.ToLowerInvariant()} location near {address}."
            : $"{name} е открита {typeLabel} локация близо до {address}.";
    }

    private static List<OverpassFilter> BuildFilters(PlaceDiscoveryRequest request)
    {
        var text = (request.QueryText ?? string.Empty).ToLowerInvariant();
        var type = request.PlaceTypeHint;
        var filters = new List<OverpassFilter>();

        if (ContainsNightlifeIntent(text) || type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar)
        {
            filters.AddRange(new[]
            {
                new OverpassFilter("amenity", "nightclub"),
                new OverpassFilter("amenity", "bar"),
                new OverpassFilter("amenity", "pub"),
                new OverpassFilter("amenity", "cafe")
            });
        }

        if (type is PlaceType.Cafe or PlaceType.Restaurant)
        {
            filters.AddRange(new[]
            {
                new OverpassFilter("amenity", "cafe"),
                new OverpassFilter("amenity", "restaurant"),
                new OverpassFilter("amenity", "bar")
            });
        }

        if (type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest)
        {
            filters.AddRange(new[]
            {
                new OverpassFilter("leisure", "park"),
                new OverpassFilter("leisure", "garden"),
                new OverpassFilter("natural", "wood")
            });
        }

        if (type is PlaceType.CulturalSite or PlaceType.Museum or PlaceType.Gallery or PlaceType.Theater or PlaceType.Cinema)
        {
            filters.AddRange(new[]
            {
                new OverpassFilter("tourism", "museum"),
                new OverpassFilter("tourism", "gallery"),
                new OverpassFilter("amenity", "theatre"),
                new OverpassFilter("amenity", "cinema"),
                new OverpassFilter("tourism", "attraction")
            });
        }

        if (type is PlaceType.Sport or PlaceType.Gym or PlaceType.Stadium)
        {
            filters.AddRange(new[]
            {
                new OverpassFilter("leisure", "sports_centre"),
                new OverpassFilter("leisure", "fitness_centre"),
                new OverpassFilter("leisure", "stadium")
            });
        }

        if (type is PlaceType.Viewpoint or PlaceType.Landmark or PlaceType.HistoricSite)
        {
            filters.AddRange(new[]
            {
                new OverpassFilter("tourism", "viewpoint"),
                new OverpassFilter("tourism", "attraction"),
                new OverpassFilter("historic", "monument")
            });
        }

        if (filters.Count == 0)
        {
            filters.AddRange(new[]
            {
                new OverpassFilter("amenity", "cafe"),
                new OverpassFilter("amenity", "restaurant"),
                new OverpassFilter("amenity", "bar"),
                new OverpassFilter("leisure", "park"),
                new OverpassFilter("tourism", "attraction")
            });
        }

        return filters
            .DistinctBy(x => $"{x.Key}:{x.Value}")
            .ToList();
    }

    private static double ScoreCandidate(DiscoveredPlaceCandidate candidate, PlaceDiscoveryRequest request)
    {
        var score = 0.5;
        var text = $"{candidate.Name} {candidate.Description} {candidate.Address}".ToLowerInvariant();
        var queryText = (request.QueryText ?? string.Empty).ToLowerInvariant();

        if (request.PlaceTypeHint is not null && candidate.Type == request.PlaceTypeHint)
            score += 0.35;

        if (ContainsNightlifeIntent(queryText))
        {
            if (candidate.Type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar) score += 0.45;
            if (candidate.Type is PlaceType.Cafe or PlaceType.Restaurant) score += 0.18;
            if (candidate.Type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest) score -= 0.22;
            if (ContainsAny(text, "club", "bar", "pub", "night", "dance", "party", "дискот", "бар", "танц", "парти", "нощ")) score += 0.18;
        }

        if (ContainsAny(queryText, "разход", "walk", "тих", "quiet", "спокой", "relax"))
        {
            if (candidate.Type is PlaceType.Park or PlaceType.Garden or PlaceType.Viewpoint or PlaceType.Forest) score += 0.28;
            if (candidate.Type is PlaceType.Nightlife or PlaceType.Bar) score -= 0.18;
        }

        if (ContainsAny(queryText, "музей", "museum", "галерия", "gallery", "излож", "theater", "теат"))
        {
            if (candidate.Type is PlaceType.Museum or PlaceType.Gallery or PlaceType.CulturalSite or PlaceType.Theater) score += 0.32;
        }

        return Math.Clamp(score, 0.1, 1.0);
    }

    private static string? TryGetString(JsonElement tags, string propertyName)
    {
        return tags.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string NormalizeName(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static bool ContainsNightlifeIntent(string text)
    {
        return ContainsAny(text, "дискот", "клуб", "club", "party", "парти", "танц", "dance", "dj", "музик", "music", "нощ", "nightlife", "bar", "бар");
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    private static string EscapeValue(string value)
    {
        return value.Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private sealed record OverpassFilter(string Key, string Value);
}
