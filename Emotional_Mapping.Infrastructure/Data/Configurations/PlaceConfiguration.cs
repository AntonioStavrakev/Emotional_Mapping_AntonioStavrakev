using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(600);
        b.Property(x => x.Address).HasMaxLength(250);
        b.Property(x => x.TagsJson).HasColumnType("longtext");
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.WebsiteUrl).HasMaxLength(500);
        b.Property(x => x.OpeningHours).HasMaxLength(200);
        b.Property(x => x.Source).HasMaxLength(50);

        b.HasIndex(x => new { x.CityId, x.Type });

        b.OwnsOne(x => x.Location, p =>
        {
            p.Property(x => x.Lat).HasColumnName("Lat").IsRequired();
            p.Property(x => x.Lng).HasColumnName("Lng").IsRequired();
        });

        b.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
        b.HasOne(x => x.District).WithMany().HasForeignKey(x => x.DistrictId);
    }   
}