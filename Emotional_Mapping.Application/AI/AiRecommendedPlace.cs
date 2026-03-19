namespace Emotional_Mapping.Application.AI;

public class AiRecommendedPlace
{
    public Guid PlaceId { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = "";
}