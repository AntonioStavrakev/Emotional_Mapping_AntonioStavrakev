using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.Services;

public class StatsService
{
    private readonly IMapRepository _maps;
    private readonly IEmotionalPointRepository _points;

    public StatsService(IMapRepository maps, IEmotionalPointRepository points)
    {
        _maps = maps;
        _points = points;
    }

    public async Task<AiUsageDashboardDto> GetAiDashboardAsync(CancellationToken ct)
    {
        var requests = await _maps.GetAllAsync(ct);

        return new AiUsageDashboardDto
        {
            TotalRequests = requests.Count,

            TopEmotions = requests
                .Where(x => x.SelectedEmotion != null)
                .GroupBy(x => x.SelectedEmotion!.ToString())
                .Select(g => new EmotionStatDto
                {
                    Emotion = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
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

    public async Task<List<DistrictEmotionScoreDto>> GetDistrictScoresAsync(Guid cityId, CancellationToken ct)
    {
        var points = await _points.GetByCityAsync(cityId, null, ct);

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
            .GroupBy(p => new 
            { 
                p.DistrictId, 
                DistrictName = p.District != null ? p.District.Name : "Unknown"
            })
            .Select(g => new DistrictEmotionScoreDto
            {
                DistrictId = g.Key.DistrictId,
                DistrictName = g.Key.DistrictName,
                PositiveScore = g.Count(x => positive.Contains(x.Emotion)),
                NegativeScore = g.Count(x => negative.Contains(x.Emotion))
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
}