namespace Emotional_Mapping.Application.DTOs;

public class MoodClusterDto
{
    public string Emotion { get; set; } = "";
    public List<string> Places { get; set; } = new();
}