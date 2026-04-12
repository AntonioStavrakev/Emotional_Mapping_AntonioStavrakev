namespace Emotional_Mapping.Application.DTOs;

public class AiUsageDashboardDto
{
    public int TotalRequests { get; set; }

    public int TotalRequestsToday { get; set; }

    public int UniqueUsersToday { get; set; }

    public int TotalMapsToday { get; set; }

    public List<EmotionStatDto> TopEmotions { get; set; } = new();
    public List<EmotionStatDto> TrendingEmotions { get; set; } = new();

    public List<UserActivityDto> TopUsers { get; set; } = new();
}
