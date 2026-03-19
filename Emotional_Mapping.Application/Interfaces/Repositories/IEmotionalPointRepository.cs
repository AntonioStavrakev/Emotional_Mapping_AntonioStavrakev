using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IEmotionalPointRepository
{
    Task AddAsync(EmotionalPoint point, CancellationToken ct);
    Task<EmotionalPoint?> GetAsync(Guid id, CancellationToken ct);
    Task<List<EmotionalPoint>> GetByCityAsync(Guid cityId, EmotionType? emotion, CancellationToken ct);
    Task DeleteAsync(EmotionalPoint point, CancellationToken ct);
}