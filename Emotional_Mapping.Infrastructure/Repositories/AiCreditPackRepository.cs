using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class AiCreditPackRepository : IAiCreditPackRepository
{
    private readonly AppDbContext _db;

    public AiCreditPackRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(AiCreditPack pack, CancellationToken ct)
        => _db.Set<AiCreditPack>().AddAsync(pack, ct).AsTask();

    public Task<AiCreditPack?> GetByStripeSessionIdAsync(string stripeSessionId, CancellationToken ct)
        => _db.Set<AiCreditPack>()
            .FirstOrDefaultAsync(x => x.StripeSessionId == stripeSessionId, ct);

    public async Task<int> GetActiveRemainingCreditsAsync(string userId, DateTime nowUtc, CancellationToken ct)
    {
        var total = await _db.Set<AiCreditPack>()
            .Where(x => x.UserId == userId && x.ExpiresAtUtc > nowUtc && x.RemainingCredits > 0)
            .SumAsync(x => (int?)x.RemainingCredits, ct);

        return total ?? 0;
    }

    public Task<DateTime?> GetNextActiveExpiryUtcAsync(string userId, DateTime nowUtc, CancellationToken ct)
        => _db.Set<AiCreditPack>()
            .Where(x => x.UserId == userId && x.ExpiresAtUtc > nowUtc && x.RemainingCredits > 0)
            .OrderBy(x => x.ExpiresAtUtc)
            .Select(x => (DateTime?)x.ExpiresAtUtc)
            .FirstOrDefaultAsync(ct);

    public Task<AiCreditPack?> GetNextActivePackAsync(string userId, DateTime nowUtc, CancellationToken ct)
        => _db.Set<AiCreditPack>()
            .Where(x => x.UserId == userId && x.ExpiresAtUtc > nowUtc && x.RemainingCredits > 0)
            .OrderBy(x => x.ExpiresAtUtc)
            .ThenBy(x => x.PurchasedAtUtc)
            .FirstOrDefaultAsync(ct);
}
