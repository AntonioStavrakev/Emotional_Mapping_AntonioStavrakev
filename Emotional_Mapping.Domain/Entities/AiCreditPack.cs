namespace Emotional_Mapping.Domain.Entities;

public class AiCreditPack
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string PackageCode { get; private set; } = null!;
    public int TotalCredits { get; private set; }
    public int RemainingCredits { get; private set; }
    public DateTime PurchasedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string Source { get; private set; } = null!;
    public string? StripeSessionId { get; private set; }

    private AiCreditPack() { }

    public AiCreditPack(
        string userId,
        string packageCode,
        int totalCredits,
        DateTime expiresAtUtc,
        string source,
        string? stripeSessionId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(packageCode)) throw new ArgumentException("PackageCode required.", nameof(packageCode));
        if (totalCredits <= 0) throw new ArgumentOutOfRangeException(nameof(totalCredits));
        if (expiresAtUtc <= DateTime.UtcNow) throw new ArgumentException("ExpiresAtUtc must be in the future.", nameof(expiresAtUtc));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source required.", nameof(source));

        Id = Guid.NewGuid();
        UserId = userId.Trim();
        PackageCode = packageCode.Trim();
        TotalCredits = totalCredits;
        RemainingCredits = totalCredits;
        PurchasedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        Source = source.Trim();
        StripeSessionId = string.IsNullOrWhiteSpace(stripeSessionId) ? null : stripeSessionId.Trim();
    }

    public bool IsActive(DateTime nowUtc) => RemainingCredits > 0 && ExpiresAtUtc > nowUtc;

    public bool TryConsumeOne(DateTime nowUtc)
    {
        if (!IsActive(nowUtc)) return false;

        RemainingCredits--;
        return true;
    }
}
