using Emotional_Mapping.Application.AI;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Infrastructure.AI;

public class RuleBasedEmotionAnalysisService : IAiEmotionService
{
    public Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisInput input, CancellationToken ct)
    {
        var text = (input.QueryText ?? "").ToLowerInvariant();

        var emotion =
            input.EmotionHint ??
            (text.Contains("спокой") ? EmotionType.Calm :
             text.Contains("радост") || text.Contains("щаст") ? EmotionType.Happy :
             text.Contains("тъг") ? EmotionType.Sad :
             text.Contains("роман") ? EmotionType.Romantic :
             text.Contains("енерг") ? EmotionType.Energetic :
             text.Contains("напреж") || text.Contains("стрес") ? EmotionType.Tension :
             EmotionType.Inspiration);

        // Прост скоринг: предпочитаме matching PlaceTypeHint; иначе по ред
        var recs = input.Places
            .Select(p => new
            {
                Place = p,
                Score = ScorePlace(p.Type, input.PlaceTypeHint, emotion)
            })
            .OrderByDescending(x => x.Score)
            .Take(25)
            .Select(x => new AiRecommendedPlace
            {
                PlaceId = x.Place.PlaceId,
                Score = x.Score,
                Reason = $"Подходящо за „{emotion}“ според локалния AI (евристики)."
            })
            .ToList();

        return Task.FromResult(new AiAnalysisResult
        {
            FinalEmotion = emotion,
            Confidence = 0.60,
            Summary = $"Локален AI модул: намерени места за настроение „{emotion}“.",
            Recommendations = recs
        });
    }

    private static double ScorePlace(PlaceType type, PlaceType? hint, EmotionType emotion)
    {
        var score = 0.5;

        if (hint is not null && type == hint) score += 0.3;

        // малко “умни” правила (примерни)
        if (emotion == EmotionType.Calm && (type == PlaceType.Park || type == PlaceType.Garden || type == PlaceType.Forest)) score += 0.2;
        if (emotion == EmotionType.Social && (type == PlaceType.Cafe || type == PlaceType.Bar || type == PlaceType.Restaurant)) score += 0.2;
        if (emotion == EmotionType.Inspiration && (type == PlaceType.Museum || type == PlaceType.Gallery || type == PlaceType.CulturalSite)) score += 0.2;

        return Math.Clamp(score, 0.1, 1.0);
    }
}