using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Places;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure.Places;

public class CompositeExternalPlaceDiscoveryService : IExternalPlaceDiscoveryService
{
    private readonly IReadOnlyList<IExternalPlaceProvider> _providers;
    private readonly ExternalPlaceDiscoveryOptions _options;

    public CompositeExternalPlaceDiscoveryService(
        IEnumerable<IExternalPlaceProvider> providers,
        IOptions<ExternalPlaceDiscoveryOptions> options)
    {
        _providers = providers.ToList();
        _options = options.Value;
    }

    public async Task<List<DiscoveredPlaceCandidate>> SearchAsync(PlaceDiscoveryRequest request, CancellationToken ct)
    {
        foreach (var provider in GetOrderedProviders())
        {
            if (!provider.IsConfigured)
                continue;

            var results = await provider.SearchAsync(request, ct);
            if (results.Count > 0)
                return results;
        }

        return new List<DiscoveredPlaceCandidate>();
    }

    private IEnumerable<IExternalPlaceProvider> GetOrderedProviders()
    {
        var map = _providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        return NormalizeProvider(_options.Provider) switch
        {
            "google" => Enumerate(map, "google", "foursquare", "osm"),
            "foursquare" => Enumerate(map, "foursquare", "google", "osm"),
            "osm" => Enumerate(map, "osm"),
            _ => Enumerate(map, "google", "foursquare", "osm")
        };
    }

    private static IEnumerable<IExternalPlaceProvider> Enumerate(
        IReadOnlyDictionary<string, IExternalPlaceProvider> providers,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (providers.TryGetValue(name, out var provider))
                yield return provider;
        }
    }

    private static string NormalizeProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider) ? "auto" : provider.Trim().ToLowerInvariant();
    }
}
