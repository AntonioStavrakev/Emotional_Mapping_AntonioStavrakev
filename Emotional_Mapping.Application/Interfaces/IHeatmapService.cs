using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Application.Interfaces;

public interface IHeatmapService
{
    string BuildHeatmapJson(
        IEnumerable<EmotionalPoint> points,
        IEnumerable<MapRecommendation> recs,
        IReadOnlyDictionary<Guid, GeoPoint>? placeLocations = null);
}
