using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.AI;

public class PlaceContextItem
{
    public Guid PlaceId { get; set; }
    public string Name { get; set; } = "";
    public PlaceType Type { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Description { get; set; }
}