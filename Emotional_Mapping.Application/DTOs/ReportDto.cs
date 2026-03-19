namespace Emotional_Mapping.Application.DTOs;

public class ReportDto
{
    public Guid? EmotionalPointId { get; set; }
    public Guid? PlaceId { get; set; }
    public string Reason { get; set; } = "";
}