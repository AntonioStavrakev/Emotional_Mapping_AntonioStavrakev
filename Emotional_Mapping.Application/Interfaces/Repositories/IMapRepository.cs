using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IMapRepository
{
    Task AddRequestAsync(MapRequest req, CancellationToken ct);
    Task AddGeneratedMapAsync(GeneratedMap map, CancellationToken ct);

    Task<GeneratedMap?> GetGeneratedMapAsync(Guid id, CancellationToken ct);
    Task<List<GeneratedMap>> GetUserMapsAsync(string userId, CancellationToken ct);
    Task<List<GeneratedMap>> GetPublicMapsAsync(Guid cityId, CancellationToken ct);
    Task<int> CountRequestsByCityAsync(Guid cityId, CancellationToken ct);
    Task<GeneratedMap?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<int> CountRequestsForUserTodayAsync(string currentUserUserId, DateTime todayStart, DateTime todayEnd, CancellationToken ct);
    Task<int> CountGeneratedMapsBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<List<MapRequest>> GetAllAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
