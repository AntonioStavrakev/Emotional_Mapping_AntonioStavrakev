using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.ValueObjects;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class BgCitiesSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var existingNames = (await context.Cities
                .AsNoTracking()
                .Select(c => c.Name)
                .ToListAsync())
            .ToHashSet();

        var cities = new List<City>
        {
            // Областни градове
            new City("София", "BG", new GeoPoint(42.6977, 23.3219), 12),
            new City("Пловдив", "BG", new GeoPoint(42.1354, 24.7453), 13),
            new City("Варна", "BG", new GeoPoint(43.2141, 27.9147), 13),
            new City("Бургас", "BG", new GeoPoint(42.5048, 27.4626), 13),
            new City("Русе", "BG", new GeoPoint(43.8356, 25.9657), 13),
            new City("Стара Загора", "BG", new GeoPoint(42.4258, 25.6345), 13),
            new City("Плевен", "BG", new GeoPoint(43.4170, 24.6067), 13),
            new City("Велико Търново", "BG", new GeoPoint(43.0757, 25.6172), 13),
            new City("Благоевград", "BG", new GeoPoint(42.0116, 23.0947), 13),
            new City("Шумен", "BG", new GeoPoint(43.2712, 26.9225), 13),
            new City("Добрич", "BG", new GeoPoint(43.5726, 27.8273), 13),
            new City("Сливен", "BG", new GeoPoint(42.6816, 26.3292), 13),
            new City("Хасково", "BG", new GeoPoint(41.9344, 25.5554), 13),
            new City("Перник", "BG", new GeoPoint(42.6050, 23.0378), 13),
            new City("Ямбол", "BG", new GeoPoint(42.4842, 26.5035), 13),
            new City("Пазарджик", "BG", new GeoPoint(42.1928, 24.3336), 13),
            new City("Враца", "BG", new GeoPoint(43.2100, 23.5528), 13),
            new City("Кюстендил", "BG", new GeoPoint(42.2833, 22.6911), 13),
            new City("Монтана", "BG", new GeoPoint(43.4085, 23.2257), 13),
            new City("Габрово", "BG", new GeoPoint(42.8742, 25.3187), 13),
            new City("Кърджали", "BG", new GeoPoint(41.6340, 25.3777), 13),
            new City("Видин", "BG", new GeoPoint(43.9910, 22.8818), 13),
            new City("Ловеч", "BG", new GeoPoint(43.1380, 24.7140), 13),
            new City("Разград", "BG", new GeoPoint(43.5276, 26.5244), 13),
            new City("Силистра", "BG", new GeoPoint(44.1174, 27.2600), 13),
            new City("Търговище", "BG", new GeoPoint(43.2467, 26.5726), 13),
            new City("Смолян", "BG", new GeoPoint(41.5774, 24.7011), 13),

            // Популярни градове
            new City("Банско", "BG", new GeoPoint(41.8383, 23.4886), 14),
            new City("Несебър", "BG", new GeoPoint(42.6592, 27.7361), 14),
            new City("Созопол", "BG", new GeoPoint(42.4172, 27.6953), 14),
            new City("Сандански", "BG", new GeoPoint(41.5667, 23.2833), 14),
            new City("Казанлък", "BG", new GeoPoint(42.6190, 25.3990), 14),
            new City("Асеновград", "BG", new GeoPoint(42.0034, 24.8716), 14),
            new City("Самоков", "BG", new GeoPoint(42.3367, 23.5565), 14),
            new City("Свищов", "BG", new GeoPoint(43.6219, 25.3521), 14),
            new City("Троян", "BG", new GeoPoint(42.8849, 24.7147), 14),
            new City("Балчик", "BG", new GeoPoint(43.4214, 28.1625), 14),
            new City("Петрич", "BG", new GeoPoint(41.3968, 23.2067), 14),
            new City("Карлово", "BG", new GeoPoint(42.6333, 24.8056), 14),
            new City("Велинград", "BG", new GeoPoint(42.0275, 23.9917), 14),
            new City("Ботевград", "BG", new GeoPoint(42.9064, 23.7928), 14),
            new City("Гоце Делчев", "BG", new GeoPoint(41.5732, 23.7277), 14),
            new City("Дупница", "BG", new GeoPoint(42.2636, 23.1149), 14),
            new City("Горна Оряховица", "BG", new GeoPoint(43.1271, 25.6958), 14),
            new City("Попово", "BG", new GeoPoint(43.3513, 26.2251), 14),
            new City("Свиленград", "BG", new GeoPoint(41.7665, 26.2028), 14),
            new City("Лом", "BG", new GeoPoint(43.8275, 23.2336), 14),
            new City("Панагюрище", "BG", new GeoPoint(42.5048, 24.1891), 14),
            new City("Харманли", "BG", new GeoPoint(41.9284, 25.9039), 14),
            new City("Нова Загора", "BG", new GeoPoint(42.4900, 26.0122), 14),
            new City("Айтос", "BG", new GeoPoint(42.7022, 27.2517), 14),
            new City("Каварна", "BG", new GeoPoint(43.4331, 28.3397), 14),
            new City("Поморие", "BG", new GeoPoint(42.5563, 27.6397), 14),
            new City("Приморско", "BG", new GeoPoint(42.2672, 27.7564), 14),
            new City("Копривщица", "BG", new GeoPoint(42.6378, 24.3497), 14)
        };

        var missingCities = cities
            .Where(c => !existingNames.Contains(c.Name))
            .ToList();

        if (missingCities.Any())
        {
            context.Cities.AddRange(missingCities);
            await context.SaveChangesAsync();
        }
    }
}
