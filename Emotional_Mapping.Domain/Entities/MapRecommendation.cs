using Emotional_Mapping.Domain.Enums;
using System.Text.Json.Serialization;

namespace Emotional_Mapping.Domain.Entities;

public class MapRecommendation
{
    public Guid Id { get; private set; }

    public Guid GeneratedMapId { get; private set; }
    [JsonIgnore]
    public GeneratedMap GeneratedMap { get; private set; } = null!;

    public Guid PlaceId { get; private set; }
    public Place Place { get; private set; } = null!;

    public EmotionType Emotion { get; private set; }
    public double Score { get; private set; }

    public int? DistanceMeters { get; private set; }
    public string? Reason { get; private set; }
    public string? MatchReasonsJson { get; private set; }
    public string? BestTimeToVisit { get; private set; }
    public int? EstimatedStayMinutes { get; private set; }

    private MapRecommendation() { }

    public MapRecommendation(
        Guid generatedMapId,
        Guid placeId,
        EmotionType emotion,
        double score,
        string? reason = null,
        int? distanceMeters = null,
        string? matchReasonsJson = null,
        string? bestTimeToVisit = null,
        int? estimatedStayMinutes = null)
    {
        if (generatedMapId == Guid.Empty) throw new ArgumentException("GeneratedMapId required.", nameof(generatedMapId));
        if (placeId == Guid.Empty) throw new ArgumentException("PlaceId required.", nameof(placeId));

        Id = Guid.NewGuid();
        GeneratedMapId = generatedMapId;
        PlaceId = placeId;
        Emotion = emotion;
        Score = Math.Clamp(score, 0, 1);

        Reason = reason;
        DistanceMeters = distanceMeters;
        MatchReasonsJson = matchReasonsJson;
        BestTimeToVisit = bestTimeToVisit;
        EstimatedStayMinutes = estimatedStayMinutes;
    }
}
