namespace Emotional_Mapping.Domain.ValueObjects;

public class GeoPoint
{
    public double Lat { get; private set; }
    public double Lng { get; private set; }

    private GeoPoint()
    {
    }

    public GeoPoint(double lat, double lng)
    {
        if (lat is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(lat));
        if (lng is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(lng));
        Lat = lat;
        Lng = lng;
    }
    
}