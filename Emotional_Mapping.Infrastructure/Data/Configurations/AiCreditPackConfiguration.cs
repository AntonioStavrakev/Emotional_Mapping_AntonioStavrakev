using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class AiCreditPackConfiguration : IEntityTypeConfiguration<AiCreditPack>
{
    public void Configure(EntityTypeBuilder<AiCreditPack> b)
    {
        b.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        b.Property(x => x.PackageCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.Source).HasMaxLength(40).IsRequired();
        b.Property(x => x.StripeSessionId).HasMaxLength(255);

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.ExpiresAtUtc);
        b.HasIndex(x => x.StripeSessionId).IsUnique();
    }
}
