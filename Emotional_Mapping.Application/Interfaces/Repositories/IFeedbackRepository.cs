using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IFeedbackRepository
{
    Task AddAsync(Feedback feedback, CancellationToken ct);
}