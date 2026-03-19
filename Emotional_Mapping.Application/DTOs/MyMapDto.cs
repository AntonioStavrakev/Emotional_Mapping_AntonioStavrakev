using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.DTOs;

public class MyMapDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public EmotionType DominantEmotion { get; set; }
    public double Confidence { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    public string? Summary { get; set; }
    public string? HeatmapJson { get; set; }

    public int RecommendationsCount { get; set; }
}