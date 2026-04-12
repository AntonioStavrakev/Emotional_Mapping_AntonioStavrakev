using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class MapRepository : IMapRepository
{
    private readonly AppDbContext _db;

    public MapRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddRequestAsync(MapRequest req, CancellationToken ct)
        => _db.MapRequests.AddAsync(req, ct).AsTask();

    public Task AddGeneratedMapAsync(GeneratedMap map, CancellationToken ct)
        => _db.GeneratedMaps.AddAsync(map, ct).AsTask();

    public Task<GeneratedMap?> GetGeneratedMapAsync(Guid id, CancellationToken ct)
    {
        return _db.GeneratedMaps
            .Include(x => x.MapRequest)
            .Include(x => x.Recommendations)
            .ThenInclude(r => r.Place)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<List<GeneratedMap>> GetUserMapsAsync(string userId, CancellationToken ct)
    {
        return _db.GeneratedMaps
            .Include(x => x.MapRequest)
            .Where(x => x.MapRequest != null && x.MapRequest.UserId == userId)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .ToListAsync(ct);
    }

    public Task<List<GeneratedMap>> GetPublicMapsAsync(Guid cityId, CancellationToken ct)
    {
        return _db.GeneratedMaps
            .Include(x => x.MapRequest)
            .Include(x => x.Recommendations)
                .ThenInclude(r => r.Place)
            .Where(x => x.MapRequest != null &&
                        x.MapRequest.CityId == cityId &&
                        x.Visibility == MapVisibility.Public)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .Take(50)
            .ToListAsync(ct);
    }

    public Task<int> CountRequestsByCityAsync(Guid cityId, CancellationToken ct)
    {
        var query = _db.MapRequests.AsQueryable();

        if (cityId != Guid.Empty)
            query = query.Where(x => x.CityId == cityId);

        return query.CountAsync(ct);
    }

    public Task<GeneratedMap?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        return _db.GeneratedMaps
            .Include(x => x.MapRequest)
            .Include(x => x.Recommendations)
            .ThenInclude(r => r.Place)
            .FirstOrDefaultAsync(x => x.PublicSlug == slug, ct);
    }

    public Task<int> CountRequestsForUserTodayAsync(
        string userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct)
    {
        return _db.MapRequests.CountAsync(x =>
            x.UserId == userId &&
            x.CreatedAtUtc >= fromUtc &&
            x.CreatedAtUtc <= toUtc, ct);
    }

    public Task<int> CountGeneratedMapsBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        return _db.GeneratedMaps.CountAsync(x =>
            x.GeneratedAtUtc >= fromUtc &&
            x.GeneratedAtUtc <= toUtc, ct);
    }

    public Task<List<MapRequest>> GetAllAsync(CancellationToken ct)
    {
        return _db.MapRequests
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
