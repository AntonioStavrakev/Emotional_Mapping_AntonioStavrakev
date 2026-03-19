using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        b.Property(x => x.Name).HasMaxLength(120).IsRequired(); 
        b.Property(x => x.Country).HasMaxLength(10).IsRequired();
        b.Property(x => x.DefaultZoom).IsRequired();

        b.OwnsOne(x => x.Center, p =>
        {
            p.Property(x => x.Lat).HasColumnName("CenterLat").IsRequired();
            p.Property(x => x.Lng).HasColumnName("CenterLng").IsRequired();
        });

        b.Navigation(x => x.Districts).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Places).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    
}