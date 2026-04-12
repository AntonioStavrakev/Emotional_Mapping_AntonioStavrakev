namespace Emotional_Mapping.Infrastructure.Places;

public class OpenStreetMapOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://overpass-api.de/api";
    public string UserAgent { get; set; } = "GEOFEEL/1.0 (local development)";
    public int DefaultRadiusMeters { get; set; } = 3500;
    public int MaxResults { get; set; } = 20;
}
