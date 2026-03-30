using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class DistrictRepository : IDistrictRepository
{
    private readonly AppDbContext _db;

    public DistrictRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<District>> GetByCityAsync(Guid cityId, CancellationToken ct)
    {
        return _db.Districts
            .Where(x => x.CityId == cityId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }
}