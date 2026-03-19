using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> b)
    {
        b.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Comment).HasMaxLength(500);

        b.HasIndex(x => x.GeneratedMapId);

        b.HasOne(x => x.GeneratedMap).WithMany().HasForeignKey(x => x.GeneratedMapId);
        b.HasOne(x => x.Recommendation).WithMany().HasForeignKey(x => x.RecommendationId);
    }
}