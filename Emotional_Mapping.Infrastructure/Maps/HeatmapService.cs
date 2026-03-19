using System.Text.Json;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Infrastructure.Maps;

public class HeatmapService : IHeatmapService
{
    
    public string BuildHeatmapJson(IEnumerable<EmotionalPoint> points, IEnumerable<MapRecommendation> recs)
    { 
        // [[lat,lng,weight], ...]
        var list = new List<double[]>();

        foreach (var p in points)
            list.Add(new[] { p.Location.Lat, p.Location.Lng, Math.Clamp(p.Intensity / 5.0, 0.1, 1.0) });

        foreach (var r in recs)
            list.Add(new[] { r.Place.Location.Lat, r.Place.Location.Lng, Math.Clamp(r.Score, 0.1, 1.0) });

        return JsonSerializer.Serialize(list);
    }
}