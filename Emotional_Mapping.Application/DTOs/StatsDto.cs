namespace Emotional_Mapping.Application.DTOs;

public class StatsDto
{
    public Guid CityId { get; set; }
    public int TotalRequests { get; set; }
    public int TotalPoints { get; set; }
    public List<KeyValuePair<string,int>> TopEmotions { get; set; } = new();
}