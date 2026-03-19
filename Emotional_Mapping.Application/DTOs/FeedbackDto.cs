using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.DTOs;

public class FeedbackDto
{
    public Guid GeneratedMapId { get; set; }
    public Guid? RecommendationId { get; set; }
    public int? Rating { get; set; }
    public UserReactionType? Reaction { get; set; }
    public string? Comment { get; set; }
}