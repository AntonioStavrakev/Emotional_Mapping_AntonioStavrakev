using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Domain.Entities;

public class EmotionalPoint
{
    public Guid Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public Guid CityId { get; private set; }
    public City City { get; private set; } = null!;

    public Guid? PlaceId { get; private set; }
    public Place? Place { get; private set; }

    public GeoPoint Location { get; private set; } = null!;

    public EmotionType Emotion { get; private set; }
    public int Intensity { get; private set; } // 1..5

    public string? Title { get; private set; }
    public string? Note { get; private set; }
    public string? TimeOfDay { get; private set; } // Morning/Afternoon/Evening/Night
    public bool IsAnonymous { get; private set; }

    public int Upvotes { get; private set; }
    public int Downvotes { get; private set; }

    public bool IsApproved { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private EmotionalPoint() { } // EF

    public EmotionalPoint(
        string userId,
        Guid cityId,
        GeoPoint location,
        EmotionType emotion,
        int intensity,
        Guid? placeId = null,
        string? title = null,
        string? note = null,
        string? timeOfDay = null,
        bool isAnonymous = false,
        bool isApproved = true)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId required.", nameof(userId));
        if (cityId == Guid.Empty) throw new ArgumentException("CityId required.", nameof(cityId));
        if (intensity is < 1 or > 5) throw new ArgumentException("Intensity must be 1..5.", nameof(intensity));

        Id = Guid.NewGuid();
        UserId = userId;
        CityId = cityId;
        PlaceId = placeId;

        Location = location ?? throw new ArgumentNullException(nameof(location));
        Emotion = emotion;
        Intensity = intensity;

        Title = title;
        Note = note;
        TimeOfDay = timeOfDay;
        IsAnonymous = isAnonymous;

        IsApproved = isApproved;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(EmotionType emotion, int intensity, string? title, string? note, string? timeOfDay, bool isAnonymous)
    {
        if (intensity is < 1 or > 5) throw new ArgumentException("Intensity must be 1..5.", nameof(intensity));

        Emotion = emotion;
        Intensity = intensity;
        Title = title;
        Note = note;
        TimeOfDay = timeOfDay;
        IsAnonymous = isAnonymous;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void VoteUp() => Upvotes++;
    public void VoteDown() => Downvotes++;

    public void Approve() => IsApproved = true;
    public void Reject() => IsApproved = false;
}