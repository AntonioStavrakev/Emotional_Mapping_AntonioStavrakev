using Emotional_Mapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Data.Seed;

public static class BgDistrictsSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken ct = default)
    {
        var cities = await context.Cities.ToListAsync(ct);
        var existingDistrictKeys = await context.Districts
            .Select(x => new { x.CityId, x.Name })
            .ToListAsync(ct);

        var existing = existingDistrictKeys
            .Select(x => $"{x.CityId}::{x.Name}")
            .ToHashSet();

        City? Get(string name) => cities.FirstOrDefault(c => c.Name == name);
        void AddIfMissing(City? city, string districtName, List<District> target)
        {
            if (city == null) return;

            var key = $"{city.Id}::{districtName}";
            if (existing.Contains(key)) return;

            target.Add(new District(city.Id, districtName));
            existing.Add(key);
        }

        var districts = new List<District>();

        // София
        var sofia = Get("София");
        if (sofia != null)
        {
            foreach (var d in new[] {
                "Средец", "Оборище", "Лозенец", "Триадица", "Красно село",
                "Студентски", "Слатина", "Изгрев", "Подуяне", "Илинден",
                "Надежда", "Люлин", "Овча купел", "Витоша", "Панчарево",
                "Нови Искър", "Банкя", "Кремиковци", "Биримирци", "Малашевци"
            })
                AddIfMissing(sofia, d, districts);
        }

        // Пловдив
        var plovdiv = Get("Пловдив");
        if (plovdiv != null)
        {
            foreach (var d in new[] {
                "Централен", "Тракия", "Столипиново", "Кършияка", "Изгрев",
                "Западен", "Южен", "Северен", "Беломорски", "Въстанически",
                "Прослав", "Крайречен", "Захарна фабрика", "Мараша", "Каменица"
            })
                AddIfMissing(plovdiv, d, districts);
        }

        // Варна
        var varna = Get("Варна");
        if (varna != null)
        {
            foreach (var d in new[] {
                "Одесос", "Приморски", "Младост", "Владиславово", "Аспарухово",
                "Чайка", "Максуда", "Трошево", "Левски", "Колхозен пазар"
            })
                AddIfMissing(varna, d, districts);
        }

        // Бургас
        var burgas = Get("Бургас");
        if (burgas != null)
        {
            foreach (var d in new[] {
                "Център", "Лазур", "Славейков", "Победа", "Меден рудник",
                "Сарафово", "Банево", "Горно Езерово", "Долно Езерово"
            })
                AddIfMissing(burgas, d, districts);
        }

        // Русе
        var ruse = Get("Русе");
        if (ruse != null)
        {
            foreach (var d in new[] {
                "Централен", "Дружба", "Чародейка", "Здравец", "Родина",
                "Ялта", "Цветница", "Средна кула"
            })
                AddIfMissing(ruse, d, districts);
        }

        // Стара Загора
        var sz = Get("Стара Загора");
        if (sz != null)
        {
            foreach (var d in new[] {
                "Център", "Железник", "Три чучура", "Казански", "Опълченски",
                "Августа Траяна"
            })
                AddIfMissing(sz, d, districts);
        }

        // Велико Търново
        var vt = Get("Велико Търново");
        if (vt != null)
        {
            foreach (var d in new[] {
                "Царевец", "Колю Фичето", "Акация", "Картала", "Малък Франкел"
            })
                AddIfMissing(vt, d, districts);
        }

        // Плевен
        var pleven = Get("Плевен");
        if (pleven != null)
        {
            foreach (var d in new[] {
                "Широка", "Дружба", "Кайлъка", "Мараша", "Б. Балабанов"
            })
                AddIfMissing(pleven, d, districts);
        }

        // Благоевград
        var blagoevgrad = Get("Благоевград");
        if (blagoevgrad != null)
        {
            foreach (var d in new[] {
                "Център", "Вароша", "Запад", "Освобождение", "Еленово"
            })
                AddIfMissing(blagoevgrad, d, districts);
        }

        if (districts.Any())
        {
            context.Districts.AddRange(districts);
            await context.SaveChangesAsync(ct);
        }
    }
}
