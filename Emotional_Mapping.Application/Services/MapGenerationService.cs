using Emotional_Mapping.Application.AI;
using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Application.Places;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Application.Services;

public class MapGenerationService
{
    private const int FreeDailyLimit = 10;
    private const int PremiumDailyLimit = 100;

    private readonly ICityRepository _cities;
    private readonly IPlaceRepository _places;
    private readonly IEmotionalPointRepository _points;
    private readonly IDistrictRepository _districts;
    private readonly IMapRepository _maps;
    private readonly IAiCreditPackRepository _creditPacks;
    private readonly IExternalPlaceDiscoveryService _externalPlaces;
    private readonly IAiEmotionService _ai;
    private readonly IHeatmapService _heatmap;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public MapGenerationService(
        ICityRepository cities,
        IPlaceRepository places,
        IEmotionalPointRepository points,
        IDistrictRepository districts,
        IMapRepository maps,
        IAiCreditPackRepository creditPacks,
        IExternalPlaceDiscoveryService externalPlaces,
        IAiEmotionService ai,
        IHeatmapService heatmap,
        IUnitOfWork uow,
        ICurrentUser currentUser)
    {
        _cities = cities;
        _places = places;
        _points = points;
        _districts = districts;
        _maps = maps;
        _creditPacks = creditPacks;
        _externalPlaces = externalPlaces;
        _ai = ai;
        _heatmap = heatmap;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<GenerateMapResultDto> GenerateAsync(GenerateMapRequestDto dto, CancellationToken ct)
    {
        var language = NormalizeLanguage(dto.Language);
        var effectivePlaceType = dto.SelectedPlaceType ?? InferPlaceTypeHint(dto.QueryText, dto.SelectedEmotion);

        if (dto.CityId == Guid.Empty)
            throw new InvalidOperationException(language == "en" ? "Select a city." : "Избери град.");

        if (_currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            var quota = await GetDailyQuotaAsync(ct);
            if (quota.RemainingToday <= 0 && quota.ExtraCreditsRemaining <= 0)
                throw new InvalidOperationException(
                    language == "en"
                        ? $"You reached the daily limit of {quota.DailyLimit} AI requests. It resets tomorrow. You can activate Premium or buy 10 extra requests valid for 5 days."
                        : $"Достигнат е дневният лимит от {quota.DailyLimit} AI заявки. Ще се рестартира утре. Можеш да активираш Premium или да купиш 10 допълнителни заявки, валидни 5 дни.");
        }

        var city = await _cities.GetAsync(dto.CityId, ct)
                   ?? throw new InvalidOperationException(language == "en" ? "Invalid city." : "Невалиден град.");

        string? districtName = null;
        if (dto.DistrictId.HasValue)
        {
            var validDistrict = await _districts.GetByCityAsync(dto.CityId, ct);
            districtName = validDistrict.FirstOrDefault(x => x.Id == dto.DistrictId.Value)?.Name;
            if (districtName is null)
                throw new InvalidOperationException(language == "en"
                    ? "The selected district does not belong to the selected city."
                    : "Избраният район не принадлежи към избрания град.");
        }

        var cachedPlaces = await _places.GetByCityAsync(dto.CityId, dto.DistrictId, effectivePlaceType, ct);
        if (cachedPlaces.Count == 0 && dto.DistrictId.HasValue)
        {
            cachedPlaces = await _places.GetByCityAsync(dto.CityId, null, effectivePlaceType, ct);
        }

        var places = await DiscoverCandidatePlacesAsync(
            city,
            districtName,
            dto.QueryText ?? "",
            dto.SelectedEmotion,
            effectivePlaceType,
            dto.RadiusMeters,
            language,
            cachedPlaces,
            ct);

        var points = await _points.GetByCityAsync(dto.CityId, dto.SelectedEmotion, ct);

        var aiInput = new AiAnalysisInput
        {
            CityName = city.Name,
            DistrictName = districtName,
            CityCenterLat = city.Center.Lat,
            CityCenterLng = city.Center.Lng,
            QueryText = dto.QueryText ?? "",
            EmotionHint = dto.SelectedEmotion,
            PlaceTypeHint = effectivePlaceType,
            RadiusMeters = dto.RadiusMeters,
            Language = language,
            Places = places.Select(p => new PlaceContextItem
            {
                PlaceId = p.Id,
                Name = p.Name,
                Type = p.Type,
                Lat = p.Location.Lat,
                Lng = p.Location.Lng,
                Description = p.Description
            }).ToList(),
            EmotionalSignals = points.Select(p => new EmotionalSignalItem
            {
                Lat = p.Location.Lat,
                Lng = p.Location.Lng,
                Emotion = p.Emotion,
                Intensity = p.Intensity
            }).ToList()
        };

        var aiResult = await _ai.AnalyzeAsync(aiInput, ct);

        if (_currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            var quota = await GetDailyQuotaAsync(ct);
            if (quota.RemainingToday <= 0)
            {
                var pack = await _creditPacks.GetNextActivePackAsync(_currentUser.UserId!, DateTime.UtcNow, ct);
                if (pack == null || !pack.TryConsumeOne(DateTime.UtcNow))
                {
                    throw new InvalidOperationException(
                        language == "en"
                            ? $"You reached the daily limit of {quota.DailyLimit} AI requests. It resets tomorrow. You can activate Premium or buy 10 extra requests valid for 5 days."
                            : $"Достигнат е дневният лимит от {quota.DailyLimit} AI заявки. Ще се рестартира утре. Можеш да активираш Premium или да купиш 10 допълнителни заявки, валидни 5 дни.");
                }
            }
        }

        if (aiResult.Recommendations.Count == 0)
        {
            aiResult.Recommendations = places.Count > 0
                ? places
                    .Take(10)
                    .Select(p => new AiRecommendedPlace
                    {
                        PlaceId = p.Id,
                        Score = 0.5,
                        Reason = language == "en"
                            ? "Suitable based on the available places and selected filters."
                            : "Подходящо според наличните места и избраните филтри."
                    })
                    .ToList()
                : new List<AiRecommendedPlace>
                {
                    new()
                    {
                        Name = language == "en" ? $"{city.Name} center" : $"{city.Name} център",
                        Type = effectivePlaceType ?? PlaceType.Landmark,
                        Lat = city.Center.Lat,
                        Lng = city.Center.Lng,
                        Description = language == "en"
                            ? $"AI fallback location in {city.Name}."
                            : $"AI fallback локация в {city.Name}.",
                        Score = 0.5,
                        Reason = language == "en"
                            ? $"Shows a central location in {city.Name} because there are no available seed places for this city."
                            : $"Показва централна локация в {city.Name}, защото няма налични seed места за този град."
                    }
                };
        }

        var req = new MapRequest(
            cityId: dto.CityId,
            queryText: dto.QueryText ?? "",
            userId: _currentUser.UserId,
            districtId: dto.DistrictId,
            selectedEmotion: dto.SelectedEmotion,
            selectedPlaceType: effectivePlaceType,
            language: language,
            radiusMeters: dto.RadiusMeters
        );

        req.SetAiInfo(aiResult.AiModel, aiResult.TokensUsed);

        await _maps.AddRequestAsync(req, ct);

        var gen = new GeneratedMap(
            mapRequestId: req.Id,
            title: language == "en" ? $"Emotional map: {city.Name}" : $"Емоционална карта: {city.Name}",
            dominantEmotion: aiResult.FinalEmotion,
            confidence: aiResult.Confidence,
            visibility: MapVisibility.Private,
            summary: aiResult.Summary
        );

        foreach (var rec in aiResult.Recommendations)
        {
            var place = rec.PlaceId == Guid.Empty
                ? null
                : places.FirstOrDefault(p => p.Id == rec.PlaceId);

            if (place is null && rec.PlaceId == Guid.Empty && !string.IsNullOrWhiteSpace(rec.Name))
            {
                var lat = rec.Lat ?? city.Center.Lat;
                var lng = rec.Lng ?? city.Center.Lng;

                place = new Place(
                    cityId: city.Id,
                    name: rec.Name.Trim(),
                    type: rec.Type,
                    location: new GeoPoint(lat, lng),
                    districtId: dto.DistrictId,
                    description: rec.Description ?? rec.Reason,
                    source: "openai",
                    isApproved: true);

                await _places.AddAsync(place, ct);
                places.Add(place);
                rec.PlaceId = place.Id;
            }

            if (place is null) continue;

            var entity = new MapRecommendation(
                generatedMapId: gen.Id,
                placeId: place.Id,
                emotion: aiResult.FinalEmotion,
                score: rec.Score,
                reason: rec.Reason
            );

            gen.AddRecommendation(entity);
        }

        var placeLocations = places.ToDictionary(x => x.Id, x => x.Location);
        gen.SetHeatmap(_heatmap.BuildHeatmapJson(points, gen.Recommendations, placeLocations));

        await _maps.AddGeneratedMapAsync(gen, ct);
        await _uow.SaveChangesAsync(ct);

        // Build recommendations DTO using in-memory places list (Place nav is not loaded on new entities)
        var recDtos = gen.Recommendations
            .Select(rec =>
            {
                var place = places.FirstOrDefault(p => p.Id == rec.PlaceId);
                return new RecommendationDto
                {
                    PlaceId = rec.PlaceId,
                    Name = place?.Name ?? "",
                    Type = place?.Type ?? PlaceType.Other,
                    Lat = place?.Location.Lat ?? 0,
                    Lng = place?.Location.Lng ?? 0,
                    Emotion = rec.Emotion,
                    Score = rec.Score,
                    Reason = rec.Reason
                };
            })
            .Where(r => r.Lat != 0 && r.Lng != 0)
            .ToList();

        return new GenerateMapResultDto
        {
            GeneratedMapId = gen.Id,
            Title = gen.Title,
            DominantEmotion = gen.DominantEmotion,
            Confidence = gen.Confidence,
            Summary = gen.Summary,
            HeatmapJson = gen.HeatmapJson,
            Recommendations = recDtos
        };
    }

    private static string NormalizeLanguage(string? language)
    {
        return string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "bg";
    }

    private async Task<List<Place>> DiscoverCandidatePlacesAsync(
        City city,
        string? districtName,
        string queryText,
        EmotionType? emotionHint,
        PlaceType? effectivePlaceType,
        int? radiusMeters,
        string language,
        List<Place> cachedPlaces,
        CancellationToken ct)
    {
        var merged = cachedPlaces.ToList();

        var discovered = await _externalPlaces.SearchAsync(new PlaceDiscoveryRequest
        {
            CityName = city.Name,
            DistrictName = districtName,
            QueryText = queryText,
            EmotionHint = emotionHint,
            PlaceTypeHint = effectivePlaceType,
            CenterLat = city.Center.Lat,
            CenterLng = city.Center.Lng,
            RadiusMeters = radiusMeters,
            Language = language
        }, ct);

        foreach (var item in discovered)
        {
            if (merged.Any(existing => RepresentsSamePlace(existing, item)))
                continue;

            var place = new Place(
                cityId: city.Id,
                name: item.Name,
                type: item.Type,
                location: new GeoPoint(item.Lat, item.Lng),
                districtId: null,
                description: item.Description,
                address: item.Address,
                source: item.Source,
                isApproved: true);

            await _places.AddAsync(place, ct);
            merged.Add(place);
        }

        return merged
            .OrderByDescending(place => ScoreExistingPlace(place, effectivePlaceType, emotionHint, queryText))
            .ThenBy(place => place.Name, StringComparer.OrdinalIgnoreCase)
            .Take(120)
            .ToList();
    }

    private static PlaceType? InferPlaceTypeHint(string? queryText, EmotionType? emotionHint)
    {
        var text = (queryText ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text))
            return InferPlaceTypeFromEmotion(emotionHint);

        if (ContainsAny(text, "дискот", "клуб", "club", "party", "парти", "танц", "dance", "dj", "музик", "music", "bar", "бар", "нощ", "nightlife"))
            return PlaceType.Nightlife;

        if (ContainsAny(text, "кафе", "cafe", "coffee", "ресторант", "restaurant", "вечеря", "dinner", "cocktail", "коктейл"))
            return PlaceType.Cafe;

        if (ContainsAny(text, "музей", "museum", "галерия", "gallery", "теат", "theater", "кино", "cinema", "излож"))
            return PlaceType.CulturalSite;

        if (ContainsAny(text, "спорт", "gym", "фитнес", "трениров", "run", "тич", "bike", "колело", "ски"))
            return PlaceType.Sport;

        if (ContainsAny(text, "разход", "walk", "парк", "park", "градина", "garden", "природ", "nature", "спокой", "тих"))
            return PlaceType.Park;

        return InferPlaceTypeFromEmotion(emotionHint);
    }

    private static PlaceType? InferPlaceTypeFromEmotion(EmotionType? emotionHint)
    {
        return emotionHint switch
        {
            EmotionType.Social or EmotionType.Excited => PlaceType.Nightlife,
            EmotionType.Energetic => PlaceType.Sport,
            EmotionType.Joy => PlaceType.Cafe,
            EmotionType.Calm or EmotionType.Relaxed or EmotionType.Safe => PlaceType.Park,
            EmotionType.Inspiration or EmotionType.Nostalgia => PlaceType.CulturalSite,
            EmotionType.Romantic => PlaceType.Viewpoint,
            _ => null
        };
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    private static bool RepresentsSamePlace(Place existing, DiscoveredPlaceCandidate candidate)
    {
        var sameName = string.Equals(
            NormalizePlaceName(existing.Name),
            NormalizePlaceName(candidate.Name),
            StringComparison.Ordinal);

        if (!sameName)
            return false;

        return DistanceMeters(existing.Location.Lat, existing.Location.Lng, candidate.Lat, candidate.Lng) <= 120;
    }

    private static string NormalizePlaceName(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static double ScoreExistingPlace(Place place, PlaceType? effectivePlaceType, EmotionType? emotionHint, string queryText)
    {
        var score = 0.5;
        var text = $"{place.Name} {place.Description} {place.Address}".ToLowerInvariant();
        var query = (queryText ?? string.Empty).ToLowerInvariant();

        if (effectivePlaceType is not null && place.Type == effectivePlaceType)
            score += 0.35;

        if (emotionHint is EmotionType.Social or EmotionType.Excited or EmotionType.Energetic)
        {
            if (place.Type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar) score += 0.28;
            if (place.Type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest) score -= 0.14;
        }

        if (ContainsAny(query, "дискот", "клуб", "club", "party", "парти", "танц", "dance", "dj", "музик", "music", "нощ", "nightlife", "bar", "бар"))
        {
            if (place.Type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar) score += 0.48;
            if (place.Type is PlaceType.Cafe or PlaceType.Restaurant or PlaceType.Street) score += 0.16;
            if (place.Type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Lake or PlaceType.Viewpoint) score -= 0.22;
            if (ContainsAny(text, "дискот", "клуб", "club", "бар", "bar", "music", "музик", "dance", "танц", "party", "парти", "нощ", "night")) score += 0.2;
        }

        if (ContainsAny(query, "разход", "walk", "спокой", "тих", "quiet", "relax", "природ", "nature"))
        {
            if (place.Type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Viewpoint) score += 0.24;
            if (place.Type is PlaceType.Nightlife or PlaceType.Bar or PlaceType.Club) score -= 0.18;
        }

        if (ContainsAny(query, "музей", "museum", "галерия", "gallery", "излож", "теат", "theater", "cinema", "кино"))
        {
            if (place.Type is PlaceType.Museum or PlaceType.Gallery or PlaceType.CulturalSite or PlaceType.Theater or PlaceType.Cinema) score += 0.28;
        }

        return Math.Clamp(score, 0.1, 1.0);
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

    public async Task<AiQuotaDto> GetDailyQuotaAsync(CancellationToken ct)
    {
        var isPremium = _currentUser.IsInRole("SuperUser") || _currentUser.IsInRole("Admin");
        var limit = isPremium ? PremiumDailyLimit : FreeDailyLimit;
        var nowUtc = DateTime.UtcNow;
        var (todayStartUtc, todayEndUtc, nextResetUtc) = GetDailyQuotaBoundsUtc();

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return new AiQuotaDto
            {
                UsedToday = 0,
                DailyLimit = limit,
                RemainingToday = limit,
                ExtraCreditsRemaining = 0,
                ExtraCreditsExpireAtUtc = null,
                NextResetAtUtc = nextResetUtc,
                UsageRatio = 0,
                IsPremium = false
            };
        }

        var usedToday = await _maps.CountRequestsForUserTodayAsync(
            _currentUser.UserId!,
            todayStartUtc,
            todayEndUtc,
            ct);

        var remaining = Math.Max(0, limit - usedToday);
        var extraCredits = await _creditPacks.GetActiveRemainingCreditsAsync(_currentUser.UserId!, nowUtc, ct);
        var extraExpiresAtUtc = await _creditPacks.GetNextActiveExpiryUtcAsync(_currentUser.UserId!, nowUtc, ct);

        return new AiQuotaDto
        {
            UsedToday = usedToday,
            DailyLimit = limit,
            RemainingToday = remaining,
            ExtraCreditsRemaining = extraCredits,
            ExtraCreditsExpireAtUtc = extraExpiresAtUtc,
            NextResetAtUtc = nextResetUtc,
            UsageRatio = limit == 0 ? 0 : Math.Clamp((double)usedToday / limit, 0, 1),
            IsPremium = isPremium
        };
    }

    private static (DateTime StartUtc, DateTime EndUtc, DateTime NextResetUtc) GetDailyQuotaBoundsUtc()
    {
        var zone = ResolveSofiaTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var localStart = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        var localNextStart = localStart.AddDays(1);
        var localEnd = localNextStart.AddTicks(-1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, zone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, zone),
            TimeZoneInfo.ConvertTimeToUtc(localNextStart, zone)
        );
    }

    private static TimeZoneInfo ResolveSofiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
