using Emotional_Mapping.Application.Places;

namespace Emotional_Mapping.Application.Interfaces;

public interface IExternalPlaceDiscoveryService
{
    Task<List<DiscoveredPlaceCandidate>> SearchAsync(PlaceDiscoveryRequest request, CancellationToken ct);
}
