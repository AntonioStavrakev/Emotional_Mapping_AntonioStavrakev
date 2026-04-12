using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Domain.Entities;

public class Place
{
    public Guid Id { get; private set; }

    public Guid CityId { get; private set; }
    public City City { get; private set; } = null!;

    public Guid? DistrictId { get; private set; }
    public District? District { get; private set; }

    public string Name { get; private set; } = null!;
    public PlaceType Type { get; private set; }
    public GeoPoint Location { get; private set; } = null!;

    public string? Description { get; private set; }
    public string? Address { get; private set; }
    public string? TagsJson { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? OpeningHours { get; private set; }

    public int? AveragePriceLevel { get; private set; } // 1..4
    public int? NoiseLevel { get; private set; }        // 1..5
    public int? CrowdLevel { get; private set; }        // 1..5
    public int? SafetyScore { get; private set; }       // 1..5

    public bool IsOutdoor { get; private set; }
    public bool IsFreeEntry { get; private set; }

    public string? Source { get; private set; } // seed/user
    public bool IsApproved { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastVerifiedAtUtc { get; private set; }

    private Place() { } // EF

    public Place(
        Guid cityId,
        string name,
        PlaceType type,
        GeoPoint location,
        Guid? districtId = null,
        string? description = null,
        string? address = null,
        string? source = "seed",
        bool isApproved = true)
    {
        if (cityId == Guid.Empty) throw new ArgumentException("CityId required.", nameof(cityId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", nameof(name));

        Id = Guid.NewGuid();
        CityId = cityId;
        DistrictId = districtId;

        Name = name.Trim();
        Type = type;
        Location = location ?? throw new ArgumentNullException(nameof(location));

        Description = description;
        Address = address;

        Source = source;
        IsApproved = isApproved;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string? description,
        string? address,
        string? tagsJson,
        string? imageUrl,
        string? websiteUrl,
        string? openingHours)
    {
        Description = description;
        Address = address;
        TagsJson = tagsJson;
        ImageUrl = imageUrl;
        WebsiteUrl = websiteUrl;
        OpeningHours = openingHours;
        LastVerifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateComfort(int? priceLevel, int? noise, int? crowd, int? safety, bool isOutdoor, bool isFreeEntry)
    {
        AveragePriceLevel = ClampNullable(priceLevel, 1, 4);
        NoiseLevel = ClampNullable(noise, 1, 5);
        CrowdLevel = ClampNullable(crowd, 1, 5);
        SafetyScore = ClampNullable(safety, 1, 5);
        IsOutdoor = isOutdoor;
        IsFreeEntry = isFreeEntry;
        LastVerifiedAtUtc = DateTime.UtcNow;
    }

    public void Approve() => IsApproved = true;
    public void Reject() => IsApproved = false;
    public void AssignDistrict(Guid? districtId) => DistrictId = districtId;

    private static int? ClampNullable(int? v, int min, int max)
        => v is null ? null : Math.Clamp(v.Value, min, max);
}
