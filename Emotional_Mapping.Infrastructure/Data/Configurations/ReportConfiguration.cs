using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.Property(x => x.ReporterUserId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.ModeratorNote).HasMaxLength(500);

        b.HasIndex(x => x.Status);

        b.HasOne(x => x.EmotionalPoint).WithMany().HasForeignKey(x => x.EmotionalPointId);
        b.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId);
    }
}