using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Data;

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
}