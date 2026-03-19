namespace Emotional_Mapping.Application.DTOs;

public class DistrictEmotionScoreDto
{
    public Guid DistrictId { get; set; }
    public string DistrictName { get; set; } = "";
    public int PositiveScore { get; set; }
    public int NegativeScore { get; set; }
    public int FinalScore => PositiveScore - NegativeScore;
}