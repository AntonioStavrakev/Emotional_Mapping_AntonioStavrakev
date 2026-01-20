using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Domain.Entities;

public class EmotionCatalogItem
{
    public Guid Id { get; private set; }
    public EmotionType Emotion { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string ColorHex { get; private set; } = "#FF0000";
    public bool IsActive { get; private set; }

    private EmotionCatalogItem() { } // EF

    public EmotionCatalogItem(EmotionType emotion, string displayName, string colorHex, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(colorHex)) colorHex = "#FF0000";

        Id = Guid.NewGuid();
        Emotion = emotion;
        DisplayName = displayName.Trim();
        ColorHex = colorHex.Trim();
        IsActive = isActive;
    }

    public void SetActive(bool active) => IsActive = active;
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("DisplayName required.", nameof(name));
        DisplayName = name.Trim();
    }
}