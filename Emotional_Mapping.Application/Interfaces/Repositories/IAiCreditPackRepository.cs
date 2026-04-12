using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IAiCreditPackRepository
{
    Task AddAsync(AiCreditPack pack, CancellationToken ct);
    Task<AiCreditPack?> GetByStripeSessionIdAsync(string stripeSessionId, CancellationToken ct);
    Task<int> GetActiveRemainingCreditsAsync(string userId, DateTime nowUtc, CancellationToken ct);
    Task<DateTime?> GetNextActiveExpiryUtcAsync(string userId, DateTime nowUtc, CancellationToken ct);
    Task<AiCreditPack?> GetNextActivePackAsync(string userId, DateTime nowUtc, CancellationToken ct);
}
