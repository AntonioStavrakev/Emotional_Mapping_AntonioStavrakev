using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.Places;

public class DiscoveredPlaceCandidate
{
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public PlaceType Type { get; set; } = PlaceType.Other;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string Source { get; set; } = "osm-overpass";
}
