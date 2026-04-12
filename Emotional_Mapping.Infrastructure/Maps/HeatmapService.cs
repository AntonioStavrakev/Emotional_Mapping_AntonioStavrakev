using System.Text.Json;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Infrastructure.Maps;

public class HeatmapService : IHeatmapService
{
    public string BuildHeatmapJson(
        IEnumerable<EmotionalPoint> points,
        IEnumerable<MapRecommendation> recs,
        IReadOnlyDictionary<Guid, GeoPoint>? placeLocations = null)
    {
        // [[lat,lng,weight], ...]
        var list = new List<double[]>();

        foreach (var p in points)
            list.Add(new[] { p.Location.Lat, p.Location.Lng, Math.Clamp(p.Intensity / 5.0, 0.1, 1.0) });

        foreach (var r in recs)
        {
            var location = r.Place?.Location;
            if (location is null && placeLocations?.TryGetValue(r.PlaceId, out var mappedLocation) == true)
                location = mappedLocation;

            if (location is null)
                continue;

            list.Add(new[] { location.Lat, location.Lng, Math.Clamp(r.Score, 0.1, 1.0) });
        }

        return JsonSerializer.Serialize(list);
    }
}
