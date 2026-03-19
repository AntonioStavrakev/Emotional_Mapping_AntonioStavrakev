using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface ICityRepository
{
    Task<List<City>> GetAllAsync(CancellationToken ct);
    Task<City?> GetAsync(Guid id, CancellationToken ct);
}