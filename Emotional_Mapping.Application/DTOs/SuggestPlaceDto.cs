using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.DTOs;

public class SuggestPlaceDto
{
    public Guid CityId { get; set; }
    public Guid? DistrictId { get; set; }
    public string Name { get; set; } = "";
    public PlaceType Type { get; set; } = PlaceType.Other;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}
