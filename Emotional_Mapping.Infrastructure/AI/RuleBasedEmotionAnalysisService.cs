using Emotional_Mapping.Application.AI;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Infrastructure.AI;

public class RuleBasedEmotionAnalysisService : IAiEmotionService
{
    public Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisInput input, CancellationToken ct)
    {
        var text = (input.QueryText ?? "").ToLowerInvariant();
        var isEnglish = string.Equals(input.Language, "en", StringComparison.OrdinalIgnoreCase);

        var emotion = DetectEmotion(input.EmotionHint, text);

        // Локален fallback скоринг, когато няма OpenAI ключ.
        var recs = input.Places
            .Select(p => new
            {
                Place = p,
                Score = ScorePlace(p.Type, p.Name, p.Description, input.PlaceTypeHint, emotion, text)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => input.Places.FindIndex(p => p.PlaceId == x.Place.PlaceId))
            .Take(25)
            .Select(x => new AiRecommendedPlace
            {
                PlaceId = x.Place.PlaceId,
                Score = x.Score,
                Reason = isEnglish
                    ? $"Suitable for \"{emotion}\" according to the local AI heuristics."
                    : $"Подходящо за „{emotion}“ според локалния AI (евристики)."
            })
            .ToList();

        return Task.FromResult(new AiAnalysisResult
        {
            FinalEmotion = emotion,
            Confidence = 0.60,
            Summary = isEnglish
                ? $"Local AI module: places were found for the mood \"{emotion}\"."
                : $"Локален AI модул: намерени места за настроение „{emotion}“.",
            Recommendations = recs
        });
    }

    public Task<string?> TranslateTextAsync(string? text, string targetLanguage, CancellationToken ct)
    {
        return Task.FromResult(text);
    }

    private static EmotionType DetectEmotion(EmotionType? hint, string text)
    {
        return hint ??
               (ContainsNightlifeIntent(text) ? EmotionType.Excited :
                text.Contains("спокой") || text.Contains("тих") ? EmotionType.Calm :
                text.Contains("calm") || text.Contains("quiet") ? EmotionType.Calm :
                text.Contains("релакс") || text.Contains("почив") ? EmotionType.Relaxed :
                text.Contains("relax") || text.Contains("rest") ? EmotionType.Relaxed :
                text.Contains("радост") || text.Contains("щаст") || text.Contains("усмив") ? EmotionType.Joy :
                text.Contains("joy") || text.Contains("happy") || text.Contains("smile") ? EmotionType.Joy :
                text.Contains("тъг") || text.Contains("меланх") ? EmotionType.Sad :
                text.Contains("sad") || text.Contains("melanch") ? EmotionType.Sad :
                text.Contains("ностал") || text.Contains("спомен") ? EmotionType.Nostalgia :
                text.Contains("nostalg") || text.Contains("memory") ? EmotionType.Nostalgia :
                text.Contains("роман") || text.Contains("любов") ? EmotionType.Romantic :
                text.Contains("roman") || text.Contains("love") ? EmotionType.Romantic :
                text.Contains("социал") || text.Contains("хора") || text.Contains("приятел") ? EmotionType.Social :
                text.Contains("social") || text.Contains("people") || text.Contains("friend") ? EmotionType.Social :
                text.Contains("енерг") || text.Contains("динами") ? EmotionType.Energetic :
                text.Contains("energet") || text.Contains("dynamic") || text.Contains("active") ? EmotionType.Energetic :
                text.Contains("напреж") || text.Contains("стрес") ? EmotionType.Tension :
                text.Contains("tension") || text.Contains("stress") ? EmotionType.Tension :
                text.Contains("тревог") ? EmotionType.Anxiety :
                text.Contains("anx") || text.Contains("worry") ? EmotionType.Anxiety :
                text.Contains("сигур") ? EmotionType.Safe :
                text.Contains("safe") || text.Contains("secure") ? EmotionType.Safe :
                text.Contains("опас") ? EmotionType.Unsafe :
                text.Contains("danger") || text.Contains("unsafe") ? EmotionType.Unsafe :
                text.Contains("вълнув") ? EmotionType.Excited :
                text.Contains("excited") || text.Contains("euphor") ? EmotionType.Excited :
                text.Contains("lonely") || text.Contains("alone") ? EmotionType.Lonely :
                EmotionType.Inspiration);
    }

    private static double ScorePlace(
        PlaceType type,
        string name,
        string? description,
        PlaceType? hint,
        EmotionType emotion,
        string queryText)
    {
        var score = 0.5;
        var text = $"{name} {description}".ToLowerInvariant();

        if (hint is not null && type == hint) score += 0.3;

        if (emotion is EmotionType.Calm or EmotionType.Relaxed or EmotionType.Safe &&
            type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Lake)
            score += 0.35;

        if (emotion is EmotionType.Joy or EmotionType.Happy or EmotionType.Excited &&
            type is PlaceType.Street or PlaceType.Park or PlaceType.CulturalSite or PlaceType.Cafe or PlaceType.Bar or PlaceType.Club or PlaceType.Nightlife)
            score += 0.28;

        if (emotion == EmotionType.Social &&
            type is PlaceType.Cafe or PlaceType.Bar or PlaceType.Restaurant or PlaceType.Nightlife or PlaceType.Street)
            score += 0.35;

        if (emotion is EmotionType.Inspiration or EmotionType.Nostalgia &&
            type is PlaceType.Museum or PlaceType.Gallery or PlaceType.CulturalSite or PlaceType.HistoricSite)
            score += 0.36;

        if (emotion == EmotionType.Romantic &&
            type is PlaceType.Park or PlaceType.RiverSide or PlaceType.Lake or PlaceType.Restaurant or PlaceType.Viewpoint)
            score += 0.34;

        if (emotion is EmotionType.Tension or EmotionType.Anxiety or EmotionType.Sad or EmotionType.Lonely &&
            type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Health)
            score += 0.25;

        if (emotion == EmotionType.Energetic &&
            type is PlaceType.Sport or PlaceType.Street or PlaceType.Nightlife or PlaceType.Bar or PlaceType.Club or PlaceType.Park)
            score += 0.33;

        if ((queryText.Contains("тих") || queryText.Contains("quiet")) && (text.Contains("парк") || text.Contains("park"))) score += 0.08;
        if ((queryText.Contains("култур") || queryText.Contains("cultur")) && (text.Contains("галерия") || text.Contains("театър") || text.Contains("музей") || text.Contains("gallery") || text.Contains("theater") || text.Contains("museum"))) score += 0.08;
        if ((queryText.Contains("разход") || queryText.Contains("walk")) && (text.Contains("парк") || text.Contains("градина") || text.Contains("улица") || text.Contains("park") || text.Contains("garden") || text.Contains("street"))) score += 0.08;
        if ((queryText.Contains("вечер") || queryText.Contains("evening") || queryText.Contains("night")) && (text.Contains("нощ") || text.Contains("бар") || text.Contains("кафе") || text.Contains("night") || text.Contains("bar") || text.Contains("cafe"))) score += 0.08;
        if (ContainsNightlifeIntent(queryText))
        {
            if (type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar) score += 0.45;
            if (type is PlaceType.Cafe or PlaceType.Restaurant or PlaceType.Street) score += 0.14;
            if (type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Lake or PlaceType.Viewpoint) score -= 0.22;
            if (ContainsAny(text, "дискот", "клуб", "club", "бар", "bar", "music", "музик", "dance", "танц", "party", "парти", "нощ", "night")) score += 0.24;
        }

        if (ContainsAny(queryText, "хора", "people", "приятел", "friend", "social", "социал") &&
            (type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar or PlaceType.Cafe))
            score += 0.12;

        return Math.Clamp(score, 0.1, 1.0);
    }

    private static bool ContainsNightlifeIntent(string text)
    {
        return ContainsAny(text, "дискот", "клуб", "club", "party", "парти", "танц", "dance", "dj", "музик", "music", "нощ", "nightlife", "бар", "bar");
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }
}
