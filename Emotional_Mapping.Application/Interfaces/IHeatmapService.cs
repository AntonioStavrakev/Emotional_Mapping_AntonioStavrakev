using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces;

public interface IHeatmapService
{
    string BuildHeatmapJson(IEnumerable<EmotionalPoint> points, IEnumerable<MapRecommendation> recs);
}