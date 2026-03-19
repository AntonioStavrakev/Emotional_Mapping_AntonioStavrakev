using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class CityRepository : ICityRepository
{
    private readonly AppDbContext _db;

    public CityRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<City>> GetAllAsync(CancellationToken ct)
    {
        return _db.Cities.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public Task<City?> GetAsync(Guid id, CancellationToken ct)
    {
        return _db.Cities.FirstOrDefaultAsync(x => x.Id == id, ct);
    }
        
}