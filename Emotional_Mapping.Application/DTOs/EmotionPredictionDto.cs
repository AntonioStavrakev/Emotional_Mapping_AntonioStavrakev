namespace Emotional_Mapping.Application.DTOs;

public class EmotionPredictionDto
{
    public string QueryText { get; set; } = "";
    public string PredictedEmotion { get; set; } = "";
    public double Confidence { get; set; }
}