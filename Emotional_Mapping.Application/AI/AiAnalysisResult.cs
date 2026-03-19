using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.AI;

public class AiAnalysisResult
{
    public EmotionType FinalEmotion { get; set; }
    public double Confidence { get; set; }
    public string? Summary { get; set; }

    public string AiModel { get; set; } = "";
    public int TokensUsed { get; set; }

    public List<AiRecommendedPlace> Recommendations { get; set; } = new();
}