using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.AI;

public class AiRecommendedPlace
{
    public Guid PlaceId { get; set; }
    public string? Name { get; set; }
    public PlaceType Type { get; set; } = PlaceType.Other;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? Description { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = "";
}
