using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken ct);
}