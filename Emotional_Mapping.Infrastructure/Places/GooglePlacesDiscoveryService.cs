using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Emotional_Mapping.Application.Places;
using Emotional_Mapping.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure.Places;

public class GooglePlacesDiscoveryService : IExternalPlaceProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly GooglePlacesOptions _options;
    private readonly ILogger<GooglePlacesDiscoveryService> _logger;

    public GooglePlacesDiscoveryService(
        IHttpClientFactory httpFactory,
        IOptions<GooglePlacesOptions> options,
        ILogger<GooglePlacesDiscoveryService> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "google";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<List<DiscoveredPlaceCandidate>> SearchAsync(PlaceDiscoveryRequest request, CancellationToken ct)
    {
        if (!IsConfigured)
            return new List<DiscoveredPlaceCandidate>();

        try
        {
            var searchRadius = Math.Clamp(request.RadiusMeters ?? 3000, 500, 8000);
            var http = _httpFactory.CreateClient("google-places");
            using var message = new HttpRequestMessage(HttpMethod.Post, "places:searchText");
            message.Headers.Add("X-Goog-Api-Key", _options.ApiKey);
            message.Headers.Add("X-Goog-FieldMask",
                "places.id,places.displayName,places.formattedAddress,places.location,places.primaryType,places.types,places.googleMapsUri,places.businessStatus");

            message.Content = JsonContent.Create(new
            {
                textQuery = BuildTextQuery(request),
                languageCode = request.Language == "bg" ? "bg" : "en",
                regionCode = "BG",
                maxResultCount = Math.Clamp(_options.MaxResults, 1, 20),
                includedType = MapGoogleIncludedType(request.PlaceTypeHint, request.QueryText),
                strictTypeFiltering = request.PlaceTypeHint is not null,
                rankPreference = "DISTANCE",
                locationRestriction = new
                {
                    circle = new
                    {
                        center = new
                        {
                            latitude = request.CenterLat,
                            longitude = request.CenterLng
                        },
                        radius = searchRadius
                    }
                }
            });

            using var response = await http.SendAsync(message, ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<GoogleTextSearchResponse>(cancellationToken: ct);
            var places = payload?.Places ?? new List<GooglePlaceItem>();

            var candidates = places
                .Where(p => p.Location is not null && !string.IsNullOrWhiteSpace(p.DisplayName?.Text))
                .Select(p => new DiscoveredPlaceCandidate
                {
                    ExternalId = $"google:{p.Id}",
                    Name = p.DisplayName!.Text!.Trim(),
                    Type = MapGooglePlaceType(p.PrimaryType, p.Types, request.PlaceTypeHint),
                    Lat = p.Location!.Latitude,
                    Lng = p.Location!.Longitude,
                    Address = p.FormattedAddress,
                    Description = BuildDescription(p),
                    Source = "google-places"
                })
                .Where(candidate => IsRelevantPlace(candidate, request, places.First(p => $"google:{p.Id}" == candidate.ExternalId), searchRadius))
                .GroupBy(x => x.Name.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation(
                "Google Places returned {TotalCount} raw results and kept {FilteredCount} relevant results for {CityName}.",
                places.Count,
                candidates.Count,
                request.CityName);

            return candidates;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Google Places discovery failed for {CityName}.", request.CityName);
            return new List<DiscoveredPlaceCandidate>();
        }
    }

    private static string BuildTextQuery(PlaceDiscoveryRequest request)
    {
        var topic = string.IsNullOrWhiteSpace(request.QueryText)
            ? request.PlaceTypeHint?.ToString() ?? "places"
            : request.QueryText.Trim();
        return $"{topic} in {request.CityName}";
    }

    private static string? MapGoogleIncludedType(PlaceType? typeHint, string queryText)
    {
        if (ContainsAny((queryText ?? string.Empty).ToLowerInvariant(), "дискот", "клуб", "club", "party", "парти", "танц", "dance", "dj", "музик", "music", "нощ", "nightlife", "bar", "бар"))
            return "night_club";

        return typeHint switch
        {
            PlaceType.Nightlife or PlaceType.Club => "night_club",
            PlaceType.Bar => "bar",
            PlaceType.Cafe => "cafe",
            PlaceType.Restaurant => "restaurant",
            PlaceType.Park => "park",
            PlaceType.Garden => "park",
            PlaceType.Museum => "museum",
            PlaceType.Gallery => "art_gallery",
            PlaceType.CulturalSite or PlaceType.Theater => "tourist_attraction",
            PlaceType.Sport or PlaceType.Gym => "gym",
            PlaceType.Viewpoint or PlaceType.Landmark or PlaceType.HistoricSite => "tourist_attraction",
            _ => null
        };
    }

    private static PlaceType MapGooglePlaceType(string? primaryType, List<string>? types, PlaceType? fallback)
    {
        var allTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(primaryType)) allTypes.Add(primaryType);
        foreach (var type in types ?? Enumerable.Empty<string>()) allTypes.Add(type);

        if (allTypes.Contains("night_club")) return PlaceType.Nightlife;
        if (allTypes.Contains("bar")) return PlaceType.Bar;
        if (allTypes.Contains("cafe")) return PlaceType.Cafe;
        if (allTypes.Contains("restaurant")) return PlaceType.Restaurant;
        if (allTypes.Contains("park")) return PlaceType.Park;
        if (allTypes.Contains("museum")) return PlaceType.Museum;
        if (allTypes.Contains("art_gallery")) return PlaceType.Gallery;
        if (allTypes.Contains("movie_theater")) return PlaceType.Cinema;
        if (allTypes.Contains("tourist_attraction")) return PlaceType.Landmark;
        if (allTypes.Contains("gym")) return PlaceType.Gym;
        if (allTypes.Contains("stadium")) return PlaceType.Stadium;
        if (allTypes.Contains("shopping_mall")) return PlaceType.Mall;
        if (allTypes.Contains("market")) return PlaceType.Market;

        return fallback ?? PlaceType.Other;
    }

    private static string? BuildDescription(GooglePlaceItem place)
    {
        var type = place.PrimaryType?.Replace('_', ' ');
        if (string.IsNullOrWhiteSpace(type))
            return place.FormattedAddress;
        if (string.IsNullOrWhiteSpace(place.FormattedAddress))
            return type;
        return $"{type}, {place.FormattedAddress}";
    }

    private static bool IsRelevantPlace(
        DiscoveredPlaceCandidate candidate,
        PlaceDiscoveryRequest request,
        GooglePlaceItem rawPlace,
        int searchRadius)
    {
        if (!string.IsNullOrWhiteSpace(rawPlace.BusinessStatus)
            && !string.Equals(rawPlace.BusinessStatus, "OPERATIONAL", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var distanceMeters = DistanceMeters(request.CenterLat, request.CenterLng, candidate.Lat, candidate.Lng);
        if (distanceMeters > searchRadius * 1.15)
            return false;

        if (request.PlaceTypeHint is not null && !IsCompatibleType(candidate.Type, request.PlaceTypeHint.Value))
            return false;

        var text = $"{candidate.Name} {candidate.Description} {candidate.Address} {rawPlace.PrimaryType} {string.Join(' ', rawPlace.Types ?? new List<string>())}"
            .ToLowerInvariant();
        var queryText = (request.QueryText ?? string.Empty).ToLowerInvariant();

        if (ContainsNightlifeIntent(queryText))
        {
            if (!IsNightlifeCandidate(candidate.Type, rawPlace.Types, text))
                return false;
        }

        if (ContainsRelaxedIntent(queryText))
        {
            if (candidate.Type is PlaceType.Nightlife or PlaceType.Bar or PlaceType.Club)
                return false;
        }

        if (ContainsCultureIntent(queryText))
        {
            if (candidate.Type is not PlaceType.Museum
                and not PlaceType.Gallery
                and not PlaceType.CulturalSite
                and not PlaceType.Theater
                and not PlaceType.Cinema
                and not PlaceType.Landmark
                && !ContainsAny(text, "museum", "gallery", "theater", "theatre", "cinema", "кино", "музей", "галерия"))
            {
                return false;
            }
        }

        if (ContainsSportIntent(queryText))
        {
            if (candidate.Type is not PlaceType.Sport and not PlaceType.Gym and not PlaceType.Stadium)
                return false;
        }

        if (IsBlockedTypeForIntent(rawPlace.Types, queryText))
            return false;

        return true;
    }

    private static bool IsCompatibleType(PlaceType actual, PlaceType requested)
    {
        if (actual == requested)
            return true;

        return requested switch
        {
            PlaceType.Nightlife => actual is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar,
            PlaceType.Club => actual is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar,
            PlaceType.Bar => actual is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar or PlaceType.Cafe,
            PlaceType.Cafe => actual is PlaceType.Cafe or PlaceType.Restaurant or PlaceType.Bar,
            PlaceType.Restaurant => actual is PlaceType.Restaurant or PlaceType.Cafe,
            PlaceType.CulturalSite => actual is PlaceType.CulturalSite or PlaceType.Museum or PlaceType.Gallery or PlaceType.Theater or PlaceType.Cinema or PlaceType.Landmark,
            PlaceType.Sport => actual is PlaceType.Sport or PlaceType.Gym or PlaceType.Stadium,
            PlaceType.Park => actual is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Viewpoint,
            PlaceType.Viewpoint => actual is PlaceType.Viewpoint or PlaceType.Landmark or PlaceType.HistoricSite,
            _ => false
        };
    }

    private static bool IsNightlifeCandidate(PlaceType placeType, List<string>? rawTypes, string text)
    {
        if (placeType is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar)
            return true;

        if (placeType is PlaceType.Cafe or PlaceType.Restaurant)
        {
            return ContainsAny(text, "club", "night", "night_club", "бар", "bar", "pub", "cocktail", "party", "dance", "dj", "music", "дискот", "парти", "танц", "музик");
        }

        return rawTypes?.Any(type => ContainsAny(type.ToLowerInvariant(), "night_club", "bar")) == true;
    }

    private static bool IsBlockedTypeForIntent(List<string>? rawTypes, string queryText)
    {
        if (rawTypes is null || rawTypes.Count == 0)
            return false;

        var lowered = rawTypes.Select(x => x.ToLowerInvariant()).ToList();

        if (ContainsNightlifeIntent(queryText))
        {
            return lowered.Any(type => type is "lodging" or "hotel" or "hostel" or "campground" or "rv_park" or "park" or "tourist_attraction" or "museum" or "church" or "school" or "university");
        }

        if (ContainsRelaxedIntent(queryText))
        {
            return lowered.Any(type => type is "night_club" or "bar");
        }

        return false;
    }

    private static bool ContainsNightlifeIntent(string text)
    {
        return ContainsAny(text, "дискот", "клуб", "club", "party", "парти", "танц", "dance", "dj", "музик", "music", "нощ", "nightlife", "bar", "бар");
    }

    private static bool ContainsRelaxedIntent(string text)
    {
        return ContainsAny(text, "разход", "walk", "спокой", "тих", "quiet", "relax", "природ", "nature", "park", "парк");
    }

    private static bool ContainsCultureIntent(string text)
    {
        return ContainsAny(text, "музей", "museum", "галерия", "gallery", "излож", "theater", "theatre", "теат", "cinema", "кино");
    }

    private static bool ContainsSportIntent(string text)
    {
        return ContainsAny(text, "спорт", "gym", "фитнес", "трениров", "run", "тич", "bike", "колело", "ски", "stadium");
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    private static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double EarthRadiusMeters = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1))
                * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double angle)
    {
        return angle * Math.PI / 180d;
    }

    private sealed class GoogleTextSearchResponse
    {
        [JsonPropertyName("places")]
        public List<GooglePlaceItem>? Places { get; set; }
    }

    private sealed class GooglePlaceItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("displayName")]
        public GoogleDisplayName? DisplayName { get; set; }

        [JsonPropertyName("formattedAddress")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("location")]
        public GoogleLocation? Location { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }

        [JsonPropertyName("businessStatus")]
        public string? BusinessStatus { get; set; }
    }

    private sealed class GoogleDisplayName
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class GoogleLocation
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
