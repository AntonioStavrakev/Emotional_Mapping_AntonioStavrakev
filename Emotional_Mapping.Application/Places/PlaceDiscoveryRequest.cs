using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.Places;

public class PlaceDiscoveryRequest
{
    public string CityName { get; set; } = "";
    public string? DistrictName { get; set; }
    public string QueryText { get; set; } = "";
    public EmotionType? EmotionHint { get; set; }
    public PlaceType? PlaceTypeHint { get; set; }
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public int? RadiusMeters { get; set; }
    public string Language { get; set; } = "bg";
}
