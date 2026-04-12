using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Data.Seed;

//служи основно за cache
public static class DbSeeder
{
    private static readonly IReadOnlyDictionary<string, string> SeedPlaceDistricts = new Dictionary<string, string>
    {
        [Key("София", "Южен парк")] = "Триадица",
        [Key("София", "НДК")] = "Триадица",
        [Key("София", "Борисова градина")] = "Изгрев",
        [Key("София", "Витошка")] = "Средец",
        [Key("София", "Александър Невски")] = "Оборище",
        [Key("София", "Докторската градина")] = "Оборище",
        [Key("София", "Национална художествена галерия")] = "Средец",
        [Key("София", "Парк Заимов")] = "Оборище",
        [Key("София", "Студентски град")] = "Студентски",
        [Key("Пловдив", "Цар-Симеоновата градина")] = "Централен",
        [Key("Пловдив", "Старият град")] = "Централен",
        [Key("Пловдив", "Капана")] = "Централен",
        [Key("Пловдив", "Античен театър")] = "Централен",
        [Key("Пловдив", "Гребна база")] = "Западен",
        [Key("Пловдив", "Главната")] = "Централен",
        [Key("Пловдив", "Тепетата")] = "Мараша",
        [Key("Варна", "Морска градина")] = "Приморски",
        [Key("Варна", "Плаж Варна")] = "Приморски",
        [Key("Варна", "Катедрала Успение Богородично")] = "Одесос",
        [Key("Варна", "Римски терми")] = "Одесос",
        [Key("Бургас", "Морска градина Бургас")] = "Лазур",
        [Key("Бургас", "Централен плаж Бургас")] = "Център",
        [Key("Бургас", "Пода")] = "Победа",
        [Key("Велико Търново", "Царевец")] = "Царевец",
        [Key("Велико Търново", "Самоводска чаршия")] = "Царевец",
        [Key("Велико Търново", "Гурко")] = "Царевец"
    };

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        var districts = await db.Districts
            .AsNoTracking()
            .Select(x => new { x.Id, x.CityId, x.Name })
            .ToListAsync(ct);

        Guid? DistrictId(City city, string placeName)
        {
            if (!SeedPlaceDistricts.TryGetValue(Key(city.Name, placeName), out var districtName))
                return null;

            return districts
                .FirstOrDefault(x => x.CityId == city.Id && x.Name == districtName)
                ?.Id;
        }

        var plovdiv = await db.Cities.FirstOrDefaultAsync(c => c.Name == "Пловдив", ct);
        var sofia = await db.Cities.FirstOrDefaultAsync(c => c.Name == "София", ct);
        var varna = await db.Cities.FirstOrDefaultAsync(c => c.Name == "Варна", ct);
        var burgas = await db.Cities.FirstOrDefaultAsync(c => c.Name == "Бургас", ct);
        var velikoTarnovo = await db.Cities.FirstOrDefaultAsync(c => c.Name == "Велико Търново", ct);

        var existingPlaceKeys = (await db.Places
                .AsNoTracking()
                .Select(x => new { x.CityId, x.Name })
                .ToListAsync(ct))
            .Select(x => $"{x.CityId}::{x.Name}")
            .ToHashSet();

        var places = new List<Place>();
        void AddSeedPlace(City? city, string name, PlaceType type, GeoPoint location, string description, string source = "seed", bool isApproved = true)
        {
            if (city == null) return;

            var placeKey = $"{city.Id}::{name}";
            if (existingPlaceKeys.Contains(placeKey)) return;

            places.Add(new Place(
                city.Id,
                name,
                type,
                location,
                districtId: DistrictId(city, name),
                description: description,
                source: source,
                isApproved: isApproved));

            existingPlaceKeys.Add(placeKey);
        }

        AddSeedPlace(sofia, "Южен парк", PlaceType.Park, new GeoPoint(42.6700, 23.3110),
            "Просторен зелен парк за разходка и спорт в южната част на София");
        AddSeedPlace(sofia, "НДК", PlaceType.CulturalSite, new GeoPoint(42.6850, 23.3180),
            "Национален дворец на културата – емблемата на София за концерти, изложби и събития");
        AddSeedPlace(sofia, "Борисова градина", PlaceType.Park, new GeoPoint(42.6732, 23.3422),
            "Най-големият и обичан парк в центъра на София");
        AddSeedPlace(sofia, "Витошка", PlaceType.Street, new GeoPoint(42.6913, 23.3200),
            "Главната пешеходна улица на София с магазини и кафенета");
        AddSeedPlace(sofia, "Александър Невски", PlaceType.HistoricSite, new GeoPoint(42.6960, 23.3328),
            "Катедралата Александър Невски – символ на София");
        AddSeedPlace(sofia, "Докторската градина", PlaceType.Park, new GeoPoint(42.6955, 23.3295),
            "Уютен малък парк в сърцето на столицата");
        AddSeedPlace(sofia, "Национална художествена галерия", PlaceType.CulturalSite, new GeoPoint(42.6965, 23.3278),
            "Художествена галерия в бившия царски дворец");
        AddSeedPlace(sofia, "Парк Заимов", PlaceType.Park, new GeoPoint(42.7040, 23.3361),
            "Тих и спокоен парк за отмора");
        AddSeedPlace(sofia, "Студентски град", PlaceType.Nightlife, new GeoPoint(42.6530, 23.3440),
            "Центърът на нощния живот за младежи");
        AddSeedPlace(sofia, "Женски пазар", PlaceType.Market, new GeoPoint(42.7060, 23.3150),
            "Автентичен открит пазар с плодове, зеленчуци и подправки");

        AddSeedPlace(plovdiv, "Цар-Симеоновата градина", PlaceType.Park, new GeoPoint(42.1410, 24.7483),
            "Централен парк в Пловдив с фонтани и зеленина");
        AddSeedPlace(plovdiv, "Старият град", PlaceType.HistoricSite, new GeoPoint(42.1476, 24.7530),
            "Магичен културно-исторически район с възрожденски къщи");
        AddSeedPlace(plovdiv, "Капана", PlaceType.CulturalSite, new GeoPoint(42.1494, 24.7488),
            "Творчески квартал с галерии, кафенета и уличен арт");
        AddSeedPlace(plovdiv, "Античен театър", PlaceType.HistoricSite, new GeoPoint(42.1472, 24.7515),
            "Римски театър от II век с невероятна гледка към града");
        AddSeedPlace(plovdiv, "Гребна база", PlaceType.Park, new GeoPoint(42.1221, 24.7553),
            "Място за отдих край водата, идеално за спокойни разходки");
        AddSeedPlace(plovdiv, "Главната", PlaceType.Street, new GeoPoint(42.1467, 24.7500),
            "Основната пешеходна зона на Пловдив");
        AddSeedPlace(plovdiv, "Тепетата", PlaceType.Park, new GeoPoint(42.1507, 24.7442),
            "Хълмовете на Пловдив – зелени оазиси с панорамни гледки");

        AddSeedPlace(varna, "Морска градина", PlaceType.Park, new GeoPoint(43.2078, 27.9390),
            "Огромен крайморски парк с алеи, кафенета и зоопарк");
        AddSeedPlace(varna, "Плаж Варна", PlaceType.Beach, new GeoPoint(43.2100, 27.9450),
            "Централният плаж на Варна");
        AddSeedPlace(varna, "Катедрала Успение Богородично", PlaceType.HistoricSite, new GeoPoint(43.2087, 27.9151),
            "Красива катедрала – символ на града");
        AddSeedPlace(varna, "Римски терми", PlaceType.HistoricSite, new GeoPoint(43.2115, 27.9167),
            "Древноримски бани – исторически паметник");

        AddSeedPlace(burgas, "Морска градина Бургас", PlaceType.Park, new GeoPoint(42.4928, 27.4803),
            "Красив крайморски парк с алеи, скулптури и детски площадки");
        AddSeedPlace(burgas, "Централен плаж Бургас", PlaceType.Beach, new GeoPoint(42.4918, 27.4850),
            "Широк пясъчен плаж в сърцето на града");
        AddSeedPlace(burgas, "Пода", PlaceType.Park, new GeoPoint(42.4578, 27.4642),
            "Защитена местност за наблюдение на птици");

        AddSeedPlace(velikoTarnovo, "Царевец", PlaceType.HistoricSite, new GeoPoint(43.0846, 25.6516),
            "Крепостта Царевец – символ на средновековна България");
        AddSeedPlace(velikoTarnovo, "Самоводска чаршия", PlaceType.CulturalSite, new GeoPoint(43.0821, 25.6326),
            "Автентичен занаятчийски квартал с ателиета и сувенири");
        AddSeedPlace(velikoTarnovo, "Гурко", PlaceType.Street, new GeoPoint(43.0794, 25.6388),
            "Живописна улица с възрожденска архитектура");

        if (places.Any())
        {
            db.Places.AddRange(places);
            await db.SaveChangesAsync(ct);
        }

        await BackfillSeedDistrictAssignmentsAsync(db, ct);

        // Seed emotion catalog
        if (!await db.EmotionCatalog.AnyAsync(ct))
        {
            db.EmotionCatalog.AddRange(
                new EmotionCatalogItem(EmotionType.Calm, "Спокойствие", "#73c9be"),
                new EmotionCatalogItem(EmotionType.Joy, "Радост", "#f4cf4b"),
                new EmotionCatalogItem(EmotionType.Nostalgia, "Носталгия", "#6d85c7"),
                new EmotionCatalogItem(EmotionType.Tension, "Напрежение", "#e77d67"),
                new EmotionCatalogItem(EmotionType.Inspiration, "Вдъхновение", "#7c6ff7"),
                new EmotionCatalogItem(EmotionType.Energetic, "Енергично", "#f59e0b"),
                new EmotionCatalogItem(EmotionType.Romantic, "Романтика", "#e87090"),
                new EmotionCatalogItem(EmotionType.Social, "Социално", "#9C27B0"),
                new EmotionCatalogItem(EmotionType.Relaxed, "Релакс", "#89d957"),
                new EmotionCatalogItem(EmotionType.Safe, "Сигурност", "#16a34a"),
                new EmotionCatalogItem(EmotionType.Excited, "Еуфория", "#c9e265")
            );

            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task BackfillSeedDistrictAssignmentsAsync(AppDbContext db, CancellationToken ct)
    {
        var places = await db.Places
            .Include(x => x.City)
            .Where(x => x.City != null)
            .ToListAsync(ct);

        var districts = await db.Districts
            .AsNoTracking()
            .ToListAsync(ct);

        var changed = false;

        foreach (var place in places)
        {
            if (!SeedPlaceDistricts.TryGetValue(Key(place.City.Name, place.Name), out var districtName))
                continue;

            var districtId = districts
                .FirstOrDefault(x => x.CityId == place.CityId && x.Name == districtName)
                ?.Id;

            if (districtId is null || place.DistrictId == districtId)
                continue;

            place.AssignDistrict(districtId);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }

    private static string Key(string cityName, string placeName) => $"{cityName}::{placeName}";
}
