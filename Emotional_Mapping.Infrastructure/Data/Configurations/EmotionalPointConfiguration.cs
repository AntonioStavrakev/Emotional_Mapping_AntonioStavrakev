using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class EmotionalPointConfiguration : IEntityTypeConfiguration<EmotionalPoint>
{
    public void Configure(EntityTypeBuilder<EmotionalPoint> b)
    {
        b.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Title).HasMaxLength(120);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.TimeOfDay).HasMaxLength(20);

        b.HasIndex(x => new { x.CityId, x.Emotion });

        b.OwnsOne(x => x.Location, p =>
        {
            p.Property(x => x.Lat).HasColumnName("Lat").IsRequired();
            p.Property(x => x.Lng).HasColumnName("Lng").IsRequired();
        });

        b.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
        b.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId);
        b.HasOne(x => x.District).WithMany().HasForeignKey(x => x.DistrictId).IsRequired(false);
    }
}