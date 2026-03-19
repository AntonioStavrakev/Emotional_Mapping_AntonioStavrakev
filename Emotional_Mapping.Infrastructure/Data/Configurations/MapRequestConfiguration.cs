using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class MapRequestConfiguration : IEntityTypeConfiguration<MapRequest>
{
    public void Configure(EntityTypeBuilder<MapRequest> b)
    {
        b.Property(x => x.QueryText).HasMaxLength(700);
        b.Property(x => x.Language).HasMaxLength(10);
        b.Property(x => x.FiltersJson).HasColumnType("longtext");
        b.Property(x => x.AiModel).HasMaxLength(80);

        b.HasIndex(x => x.CityId);
        b.HasIndex(x => x.UserId);

        b.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
        b.HasOne(x => x.District).WithMany().HasForeignKey(x => x.DistrictId);
    }
}