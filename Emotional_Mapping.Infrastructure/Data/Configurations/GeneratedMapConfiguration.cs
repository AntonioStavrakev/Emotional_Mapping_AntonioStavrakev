using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class GeneratedMapConfiguration : IEntityTypeConfiguration<GeneratedMap>
{
    public void Configure(EntityTypeBuilder<GeneratedMap> b)
    {
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Summary).HasMaxLength(800);
        b.Property(x => x.PublicSlug).HasMaxLength(200);
        b.Property(x => x.HeatmapJson).HasColumnType("text");

        b.Navigation(x => x.Recommendations).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasOne(x => x.MapRequest).WithMany().HasForeignKey(x => x.MapRequestId);
    }
}