using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.AI;

public class EmotionalSignalItem
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public EmotionType Emotion { get; set; }
    public int Intensity { get; set; }
}