using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IPlaceRepository
{
    Task<List<Place>> GetByCityAsync(Guid cityId, Guid? districtId, PlaceType? type, CancellationToken ct);
    Task<Place?> GetAsync(Guid id, CancellationToken ct);
}