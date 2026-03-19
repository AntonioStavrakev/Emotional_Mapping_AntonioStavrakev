using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.DTOs;

public class GenerateMapResultDto
{
    public Guid GeneratedMapId { get; set; }
    public string Title { get; set; } = "";
    public EmotionType DominantEmotion { get; set; }
    public double Confidence { get; set; }
    public string? Summary { get; set; }
    public string? HeatmapJson { get; set; }
    public List<RecommendationDto> Recommendations { get; set; } = new();
}