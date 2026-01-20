using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Domain.Entities;

public class City
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;
    public string Country { get; private set; } = "BG";
    public GeoPoint Center { get; private set; } = null!;
    public int DefaultZoom { get; private set; }

    private readonly List<District> _districts = new();
    public IReadOnlyCollection<District> Districts => _districts;

    private readonly List<Place> _places = new();
    public IReadOnlyCollection<Place> Places => _places;

    private City() { }

    public City(string name, string country, GeoPoint center, int defaultZoom = 12)
    {
        Id = Guid.NewGuid();

        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("City name is required.", nameof(name))
            : name.Trim();

        Country = string.IsNullOrWhiteSpace(country) ? "BG" : country.Trim();
        Center = center ?? throw new ArgumentNullException(nameof(center));
        DefaultZoom = defaultZoom is < 1 or > 20 ? 12 : defaultZoom;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("City name is required.", nameof(newName));

        Name = newName.Trim();
    }

    public void ChangeCenter(GeoPoint newCenter)
        => Center = newCenter ?? throw new ArgumentNullException(nameof(newCenter));

    public void ChangeDefaultZoom(int zoom)
        => DefaultZoom = zoom is < 1 or > 20 ? DefaultZoom : zoom;

    public void AddDistrict(District district) => _districts.Add(district);
    public void AddPlace(Place place) => _places.Add(place);
}