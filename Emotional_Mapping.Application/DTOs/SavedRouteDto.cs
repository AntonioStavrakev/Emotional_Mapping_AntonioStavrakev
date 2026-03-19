namespace Emotional_Mapping.Application.DTOs;

public class SavedRouteDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string RouteJson { get; set; } = "";
}