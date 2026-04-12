namespace Emotional_Mapping.Application.DTOs;

public class AiQuotaDto
{
    public int UsedToday { get; set; }
    public int DailyLimit { get; set; }
    public int RemainingToday { get; set; }
    public int ExtraCreditsRemaining { get; set; }
    public DateTime? ExtraCreditsExpireAtUtc { get; set; }
    public DateTime NextResetAtUtc { get; set; }
    public double UsageRatio { get; set; }
    public bool IsPremium { get; set; }
}
