namespace Emotional_Mapping.Infrastructure.Places;

public class FoursquarePlacesOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.foursquare.com/v3/places";
    public int MaxResults { get; set; } = 10;
    public string ApiVersion { get; set; } = "";
}
