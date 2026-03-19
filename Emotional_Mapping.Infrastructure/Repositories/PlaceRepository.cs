using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class PlaceRepository : IPlaceRepository
{
    private readonly AppDbContext _db;

    public PlaceRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<List<Place>> GetByCityAsync(Guid cityId, Guid? districtId, PlaceType? type, CancellationToken ct)
    {
        var q = _db.Places.AsQueryable().Where(x => x.CityId == cityId && x.IsApproved);

        if (districtId is not null) q = q.Where(x => x.DistrictId == districtId);
        if (type is not null) q = q.Where(x => x.Type == type);

        return await q.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Place?> GetAsync(Guid id, CancellationToken ct)
    {
        return _db.Places.FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}