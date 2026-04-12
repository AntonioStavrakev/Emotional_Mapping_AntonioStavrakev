using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.Services;

public class StatsService
{
    private readonly IMapRepository _maps;
    private readonly IEmotionalPointRepository _points;
    private readonly IPlaceRepository _places;

    public StatsService(IMapRepository maps, IEmotionalPointRepository points, IPlaceRepository places)
    {
        _maps = maps;
        _points = points;
        _places = places;
    }

    public async Task<AiUsageDashboardDto> GetAiDashboardAsync(CancellationToken ct)
    {
        var requests = await _maps.GetAllAsync(ct);
        var points = await _points.GetByCityAsync(Guid.Empty, null, ct);
        var (todayStartUtc, todayEndUtc) = GetTodayBoundsUtc();

        var requestsToday = requests
            .Where(x => x.CreatedAtUtc >= todayStartUtc && x.CreatedAtUtc <= todayEndUtc)
            .ToList();

        var mapsToday = await _maps.CountGeneratedMapsBetweenAsync(todayStartUtc, todayEndUtc, ct);

        return new AiUsageDashboardDto
        {
            TotalRequests = requests.Count,
            TotalRequestsToday = requestsToday.Count,
            UniqueUsersToday = requestsToday
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .Select(x => x.UserId!)
                .Distinct()
                .Count(),
            TotalMapsToday = mapsToday,

            TopEmotions = requests
                .Where(x => x.SelectedEmotion.HasValue && Enum.IsDefined(typeof(EmotionType), x.SelectedEmotion.Value))
                .GroupBy(x => x.SelectedEmotion!.ToString())
                .Select(g => new EmotionStatDto
                {
                    Emotion = g.Key ?? "Unknown",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(7)
                .ToList(),

            TrendingEmotions = points
                .Where(x => IsValidEmotion(x.Emotion))
                .GroupBy(x => x.Emotion.ToString())
                .Select(g => new EmotionStatDto
                {
                    Emotion = g.Key ?? "Unknown",
                    Count = g.Count()
                })
                .Where(x => x.Count > 0 && IsValidEmotionName(x.Emotion))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Emotion, StringComparer.OrdinalIgnoreCase)
                .Take(7)
                .ToList(),

            TopUsers = requests
                .Where(x => x.UserId != null)
                .GroupBy(x => x.UserId!)
                .Select(g => new UserActivityDto
                {
                    UserId = g.Key,
                    Requests = g.Count()
                })
                .OrderByDescending(x => x.Requests)
                .Take(5)
                .ToList()
        };
    }

    private static bool IsValidEmotion(EmotionType emotion)
    {
        return Enum.IsDefined(typeof(EmotionType), emotion) && Convert.ToInt32(emotion) > 0;
    }

    private static bool IsValidEmotionName(string? emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion))
            return false;

        return Enum.TryParse<EmotionType>(emotion, ignoreCase: true, out var parsed)
               && IsValidEmotion(parsed);
    }

    public async Task<List<DistrictEmotionScoreDto>> GetDistrictScoresAsync(Guid cityId, CancellationToken ct)
    {
        var points = await _points.GetByCityAsync(cityId, null, ct);
        var places = await _places.GetByCityAsync(cityId, null, null, ct);
        var districtPlaces = places
            .Where(x => x.DistrictId.HasValue && x.District != null)
            .ToList();

        var positive = new[]
        {
            EmotionType.Joy,
            EmotionType.Calm,
            EmotionType.Inspiration,
            EmotionType.Romantic
        };

        var negative = new[]
        {
            EmotionType.Sad,
            EmotionType.Tension
        };

        return points
            .Select(point =>
            {
                var districtId = point.DistrictId;
                var districtName = point.District?.Name;

                if (!districtId.HasValue && districtPlaces.Count > 0)
                {
                    var nearestPlace = districtPlaces
                        .Select(place => new
                        {
                            Place = place,
                            DistanceMeters = DistanceMeters(
                                point.Location.Lat,
                                point.Location.Lng,
                                place.Location.Lat,
                                place.Location.Lng)
                        })
                        .Where(x => x.DistanceMeters <= 4000)
                        .OrderBy(x => x.DistanceMeters)
                        .FirstOrDefault();

                    if (nearestPlace?.Place.DistrictId != null)
                    {
                        districtId = nearestPlace.Place.DistrictId;
                        districtName = nearestPlace.Place.District?.Name;
                    }
                }

                return new
                {
                    Point = point,
                    DistrictId = districtId,
                    DistrictName = string.IsNullOrWhiteSpace(districtName) ? "Неразпознат район" : districtName
                };
            })
            .Where(x => x.DistrictId.HasValue)
            .GroupBy(x => new
            {
                DistrictId = x.DistrictId!.Value,
                x.DistrictName
            })
            .Select(g => new DistrictEmotionScoreDto
            {
                DistrictId = g.Key.DistrictId,
                DistrictName = g.Key.DistrictName,
                PositiveScore = g.Count(x => positive.Contains(x.Point.Emotion)),
                NegativeScore = g.Count(x => negative.Contains(x.Point.Emotion))
            })
            .OrderByDescending(x => x.FinalScore)
            .ToList();
    }

    public async Task<StatsDto> GetAsync(Guid cityId, CancellationToken ct)
    {
        var totalRequests = await _maps.CountRequestsByCityAsync(cityId, ct);
        var allPoints = await _points.GetByCityAsync(cityId, null, ct);

        return new StatsDto
        {
            CityId = cityId,
            TotalRequests = totalRequests,
            TotalPoints = allPoints.Count,
            TopEmotions = allPoints
                .GroupBy(p => p.Emotion.ToString())
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .ToList()
        };
    }
    public async Task<List<EmotionTimelineDto>> GetTimelineAsync(Guid cityId, CancellationToken ct)
    {
        var points = await _points.GetByCityAsync(cityId, null, ct);

        return points
            .GroupBy(p => GetTimeOfDay(p.CreatedAtUtc))
            .SelectMany(g => g.GroupBy(x => x.Emotion.ToString())
                .Select(inner => new EmotionTimelineDto
                {
                    TimeLabel = g.Key,
                    Emotion = inner.Key,
                    Count = inner.Count()
                }))
            .OrderBy(x => x.TimeLabel)
            .ToList();
    }

    private string GetTimeOfDay(DateTime date)
    {
        var hour = date.Hour;

        if (hour < 12) return "Сутрин";
        if (hour < 18) return "Следобед";
        return "Вечер";
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetTodayBoundsUtc()
    {
        var zone = ResolveSofiaTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var localStart = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1).AddTicks(-1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, zone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, zone)
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

    private static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6371000;

        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return 2 * earthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
