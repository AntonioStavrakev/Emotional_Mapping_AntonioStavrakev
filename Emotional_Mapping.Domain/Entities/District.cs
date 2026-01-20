namespace Emotional_Mapping.Domain.Entities;

public class District
{
    public Guid Id { get; private set; }

    public Guid CityId { get; private set; }
    public City City { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public string? PolygonGeoJson { get; private set; }

    private District() { }

    public District(Guid cityId, string name, string? polygonGeoJson = null)
    {
        if (cityId == Guid.Empty) throw new ArgumentException("CityId required.", nameof(cityId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", nameof(name));

        Id = Guid.NewGuid();
        CityId = cityId;
        Name = name.Trim();
        PolygonGeoJson = polygonGeoJson;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name required.", nameof(newName));
        Name = newName.Trim();
    }

    public void SetPolygon(string? geoJson) => PolygonGeoJson = geoJson;
}