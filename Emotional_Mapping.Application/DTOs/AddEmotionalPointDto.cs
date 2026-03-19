using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.DTOs;

public class AddEmotionalPointDto
{
    public Guid CityId { get; set; }
    public Guid? PlaceId { get; set; }

    public double Lat { get; set; }
    public double Lng { get; set; }

    public EmotionType Emotion { get; set; }
    public int Intensity { get; set; } // 1..5

    public string? Title { get; set; }
    public string? Note { get; set; }
    public string? TimeOfDay { get; set; }
    public bool IsAnonymous { get; set; }
}