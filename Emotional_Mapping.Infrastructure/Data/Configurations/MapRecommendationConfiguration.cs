using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class MapRecommendationConfiguration : IEntityTypeConfiguration<MapRecommendation>
{
    public void Configure(EntityTypeBuilder<MapRecommendation> b)
    {
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.MatchReasonsJson).HasColumnType("text");
        b.Property(x => x.BestTimeToVisit).HasMaxLength(50);

        b.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId);
    }
}
