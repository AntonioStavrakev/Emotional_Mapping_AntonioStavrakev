using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.DTOs;

public class GenerateMapRequestDto
{
    public Guid CityId { get; set; }
    public Guid? DistrictId { get; set; }

    public string? QueryText { get; set; }
    public EmotionType? SelectedEmotion { get; set; }
    public PlaceType? SelectedPlaceType { get; set; }

    public int? RadiusMeters { get; set; }
    public string? Language { get; set; }
}
