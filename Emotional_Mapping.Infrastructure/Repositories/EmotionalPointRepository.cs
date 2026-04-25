using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class EmotionalPointRepository : IEmotionalPointRepository
{
    private readonly AppDbContext _db;

    public EmotionalPointRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(EmotionalPoint point, CancellationToken ct)
    {
        return _db.EmotionalPoints.AddAsync(point, ct).AsTask();
    }

    public Task<EmotionalPoint?> GetAsync(Guid id, CancellationToken ct)
    { 
        return _db.EmotionalPoints.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<EmotionalPoint>> GetByCityAsync(Guid cityId, EmotionType? emotion, CancellationToken ct)
    {
        var q = _db.EmotionalPoints
            .Include(x => x.District)
            .AsQueryable()
            .Where(x => x.IsApproved);

        if (cityId != Guid.Empty)
            q = q.Where(x => x.CityId == cityId);

        if (emotion is not null) q = q.Where(x => x.Emotion == emotion);
        return await q.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    }

    public async Task<List<EmotionalPoint>> GetPendingAsync(CancellationToken ct)
    {
        return await _db.EmotionalPoints
            .AsNoTracking()
            .Include(x => x.City)
            .Include(x => x.Place)
            .Include(x => x.District)
            .Where(x => !x.IsApproved)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public Task DeleteAsync(EmotionalPoint point, CancellationToken ct)
    {
        _db.EmotionalPoints.Remove(point);
        return Task.CompletedTask;
    }
}
