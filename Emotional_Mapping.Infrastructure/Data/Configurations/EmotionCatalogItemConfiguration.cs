using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emotional_Mapping.Infrastructure.Data.Configurations;

public class EmotionCatalogItemConfiguration : IEntityTypeConfiguration<EmotionCatalogItem>
{
    public void Configure(EntityTypeBuilder<EmotionCatalogItem> b)
    {
        b.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
        b.Property(x => x.ColorHex).HasMaxLength(12).IsRequired();
        b.HasIndex(x => x.Emotion).IsUnique();
    }
}