using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.AI;

public class AiAnalysisInput
{
    public string QueryText { get; set; } = "";
    public EmotionType? EmotionHint { get; set; }
    public PlaceType? PlaceTypeHint { get; set; }

    public string CityName { get; set; } = "";
    public string? DistrictName { get; set; }
    public double CityCenterLat { get; set; }
    public double CityCenterLng { get; set; }

    public List<PlaceContextItem> Places { get; set; } = new();
    public List<EmotionalSignalItem> EmotionalSignals { get; set; } = new();
    public int? RadiusMeters { get; set; }
    public string Language { get; set; } = "bg";
}
