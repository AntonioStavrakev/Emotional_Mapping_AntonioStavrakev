using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Domain.Entities;

public class Feedback
{
    public Guid Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public Guid GeneratedMapId { get; private set; }
    public GeneratedMap GeneratedMap { get; private set; } = null!;

    public Guid? RecommendationId { get; private set; }
    public MapRecommendation? Recommendation { get; private set; }

    public int? Rating { get; private set; }
    public UserReactionType? Reaction { get; private set; }

    public string? Comment { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Feedback() { }

    public Feedback(
        string userId,
        Guid generatedMapId,
        Guid? recommendationId,
        int? rating,
        UserReactionType? reaction,
        string? comment)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId required.", nameof(userId));
        if (generatedMapId == Guid.Empty) throw new ArgumentException("GeneratedMapId required.", nameof(generatedMapId));

        Id = Guid.NewGuid();
        UserId = userId;
        GeneratedMapId = generatedMapId;
        RecommendationId = recommendationId;

        Rating = rating is null ? null : Math.Clamp(rating.Value, 1, 5);
        Reaction = reaction;
        Comment = comment;

        CreatedAtUtc = DateTime.UtcNow;
    }
}