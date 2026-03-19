using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class SavedRouteRepository : ISavedRouteRepository
{
    private readonly AppDbContext _db;

    public SavedRouteRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(SavedRoute route, CancellationToken ct)
    {
        return _db.SavedRoutes.AddAsync(route, ct).AsTask();
    }

    public Task<List<SavedRoute>> GetByUserAsync(string userId, CancellationToken ct)
    {
        return _db.SavedRoutes
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }
}