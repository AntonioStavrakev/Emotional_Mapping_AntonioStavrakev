using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Cities.AnyAsync(ct))
        {
            var plovdiv = new City("Пловдив", "BG", new GeoPoint(42.1354, 24.7453), 13);
            var sofia = new City("София", "BG", new GeoPoint(42.6977, 23.3219), 12);

            db.Cities.AddRange(plovdiv, sofia);
            await db.SaveChangesAsync(ct);

            db.Places.AddRange(
                new Place(plovdiv.Id, "Цар-Симеоновата градина", PlaceType.Park, new GeoPoint(42.1410, 24.7483),
                    description: "Централен парк в Пловдив", source: "seed", isApproved: true),
                new Place(plovdiv.Id, "Старият град", PlaceType.HistoricSite, new GeoPoint(42.1476, 24.7530),
                    description: "Културно-исторически район", source: "seed", isApproved: true),

                new Place(sofia.Id, "Южен парк", PlaceType.Park, new GeoPoint(42.6700, 23.3110),
                    description: "Зелено място за разходка", source: "seed", isApproved: true),
                new Place(sofia.Id, "НДК", PlaceType.CulturalSite, new GeoPoint(42.6850, 23.3180),
                    description: "Културни събития и концерти", source: "seed", isApproved: true)
            );

            db.EmotionCatalog.AddRange(
                new EmotionCatalogItem(EmotionType.Calm, "Спокойствие", "#4CAF50"),
                new EmotionCatalogItem(EmotionType.Joy, "Радост", "#FFC107"),
                new EmotionCatalogItem(EmotionType.Inspiration, "Вдъхновение", "#2196F3"),
                new EmotionCatalogItem(EmotionType.Romantic, "Романтично", "#E91E63"),
                new EmotionCatalogItem(EmotionType.Social, "Социално", "#9C27B0")
            );

            await db.SaveChangesAsync(ct);
        }
    }
}