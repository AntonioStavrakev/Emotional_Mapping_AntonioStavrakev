using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Domain.Entities;

public class GeneratedMap
{
    public Guid Id { get; private set; }

    public Guid MapRequestId { get; private set; }
    public MapRequest MapRequest { get; private set; } = null!;

    public string Title { get; private set; } = "Emotional Map";
    public MapVisibility Visibility { get; private set; } = MapVisibility.Private;

    public EmotionType DominantEmotion { get; private set; }
    public double Confidence { get; private set; }
    public string? Summary { get; private set; }
    public string? HeatmapJson { get; private set; }
    public string? PublicSlug { get; private set; }
    public string? ShareToken { get; private set; }

    public DateTime GeneratedAtUtc { get; private set; }

    private readonly List<MapRecommendation> _recommendations = new();
    public IReadOnlyCollection<MapRecommendation> Recommendations => _recommendations;

    private GeneratedMap() { }

    public GeneratedMap(
        Guid mapRequestId,
        string title,
        EmotionType dominantEmotion,
        double confidence,
        MapVisibility visibility = MapVisibility.Private,
        string? summary = null,
        string? heatmapJson = null,
        string? publicSlug = null,
        string? shareToken = null)
    {
        if (mapRequestId == Guid.Empty) throw new ArgumentException("MapRequestId required.", nameof(mapRequestId));

        Id = Guid.NewGuid();
        MapRequestId = mapRequestId;
        Title = string.IsNullOrWhiteSpace(title) ? "Emotional Map" : title.Trim();
        DominantEmotion = dominantEmotion;
        Confidence = Math.Clamp(confidence, 0, 1);
        Visibility = visibility;
        Summary = summary;
        HeatmapJson = heatmapJson;
        PublicSlug = publicSlug;
        GeneratedAtUtc = DateTime.UtcNow;
        ShareToken = shareToken;
    }

    public void SetHeatmap(string? heatmapJson) => HeatmapJson = heatmapJson;

    public void AddRecommendation(MapRecommendation rec) => _recommendations.Add(rec);

    public void SetVisibility(MapVisibility v) => Visibility = v;

    public void Publish(string slug)
    {
        Visibility = MapVisibility.Public;
        PublicSlug = slug;
    }

    public void MakePrivate()
    {
        Visibility = MapVisibility.Private;
        PublicSlug = null;
    }

    public void MakePublic()
    {
        Visibility = MapVisibility.Public;
        ShareToken = Guid.NewGuid().ToString("N");
    }
}