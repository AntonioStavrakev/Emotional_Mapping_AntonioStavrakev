using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.DTOs;

public class RecommendationDto
{
    public Guid PlaceId { get; set; }
    public string Name { get; set; } = "";
    public PlaceType Type { get; set; }

    public double Lat { get; set; }
    public double Lng { get; set; }

    public EmotionType Emotion { get; set; }
    public double Score { get; set; }
    public string? Reason { get; set; }
}