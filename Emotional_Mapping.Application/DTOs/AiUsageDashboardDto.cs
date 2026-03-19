namespace Emotional_Mapping.Application.DTOs;

public class AiUsageDashboardDto
{
    public int TotalRequests { get; set; }

    public List<EmotionStatDto> TopEmotions { get; set; } = new();

    public List<UserActivityDto> TopUsers { get; set; } = new();
}