using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface ISavedRouteRepository
{
    Task AddAsync(SavedRoute route, CancellationToken ct);
    Task<List<SavedRoute>> GetByUserAsync(string userId, CancellationToken ct);
}