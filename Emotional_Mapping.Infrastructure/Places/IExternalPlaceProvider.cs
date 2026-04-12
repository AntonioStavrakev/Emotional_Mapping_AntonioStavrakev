using Emotional_Mapping.Application.Places;

namespace Emotional_Mapping.Infrastructure.Places;

public interface IExternalPlaceProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<List<DiscoveredPlaceCandidate>> SearchAsync(PlaceDiscoveryRequest request, CancellationToken ct);
}
