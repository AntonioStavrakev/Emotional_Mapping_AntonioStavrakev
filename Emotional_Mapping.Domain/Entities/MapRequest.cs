using System.ComponentModel.DataAnnotations.Schema;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Domain.Entities;

public class MapRequest
{
    public Guid Id { get; private set; }

    public string? UserId { get; private set; } // null for guest

    public Guid CityId { get; private set; }
    public City City { get; private set; } = null!;

    public Guid? DistrictId { get; private set; }
    public District? District { get; private set; }

    public string QueryText { get; private set; } = "";
    public EmotionType? SelectedEmotion { get; private set; }
    public PlaceType? SelectedPlaceType { get; private set; }

    public string Language { get; private set; } = "bg";
    public int? RadiusMeters { get; private set; }
    public double? UserLat { get; private set; }
    public double? UserLng { get; private set; }
    
    [Column(TypeName = "jsonb")] 
    public string? FiltersJson { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? AiModel { get; private set; }
    public int TokensUsed { get; private set; }

    private MapRequest() { } // EF

    public MapRequest(
        Guid cityId,
        string queryText,
        string? userId = null,
        Guid? districtId = null,
        EmotionType? selectedEmotion = null,
        PlaceType? selectedPlaceType = null,
        string language = "bg",
        int? radiusMeters = null,
        double? userLat = null,
        double? userLng = null,
        string? filtersJson = null)
    {
        if (cityId == Guid.Empty) throw new ArgumentException("CityId required.", nameof(cityId));

        Id = Guid.NewGuid();
        CityId = cityId;
        DistrictId = districtId;
        UserId = userId;

        QueryText = queryText ?? "";
        SelectedEmotion = selectedEmotion;
        SelectedPlaceType = selectedPlaceType;

        Language = string.IsNullOrWhiteSpace(language) ? "bg" : language.Trim();
        RadiusMeters = radiusMeters;
        UserLat = userLat;
        UserLng = userLng;
        FiltersJson = filtersJson;

        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetAiInfo(string model, int tokens)
    {
        AiModel = model;
        TokensUsed = tokens;
    }
}