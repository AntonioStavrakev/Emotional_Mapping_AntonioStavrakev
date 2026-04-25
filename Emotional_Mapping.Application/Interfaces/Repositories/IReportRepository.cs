using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken ct);
    Task<Report?> GetAsync(Guid id, CancellationToken ct);
    Task<List<Report>> GetActiveAsync(CancellationToken ct);
}
