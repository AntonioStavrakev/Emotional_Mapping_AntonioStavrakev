namespace Emotional_Mapping.Infrastructure.Places;

public class GooglePlacesOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://places.googleapis.com/v1";
    public int MaxResults { get; set; } = 10;
}
