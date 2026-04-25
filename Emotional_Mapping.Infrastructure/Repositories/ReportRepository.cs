using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Data;
using Emotional_Mapping.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;

    public ReportRepository(AppDbContext db)
    {
        _db = db;
    }
    public Task AddAsync(Report report, CancellationToken ct)
    {
        return _db.Reports.AddAsync(report, ct).AsTask();
    }

    public Task<Report?> GetAsync(Guid id, CancellationToken ct)
    {
        return _db.Reports
            .Include(x => x.EmotionalPoint)
            .Include(x => x.Place)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<List<Report>> GetActiveAsync(CancellationToken ct)
    {
        return _db.Reports
            .AsNoTracking()
            .Include(x => x.EmotionalPoint)
            .Include(x => x.Place)
            .Where(x => x.Status == ReportStatus.Open || x.Status == ReportStatus.InReview)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }
}
