using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IDistrictRepository
{
    Task<List<District>> GetByCityAsync(Guid cityId, CancellationToken ct);
}