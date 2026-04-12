namespace Emotional_Mapping.Application.DTOs;

public class UserActivityDto
{
    public string UserId { get; set; } = "";
    public string? UserEmail { get; set; }
    public int Requests { get; set; }
}