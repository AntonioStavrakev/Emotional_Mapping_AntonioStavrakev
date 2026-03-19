namespace Emotional_Mapping.Domain.Entities;

public class SavedRoute
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string RouteJson { get; private set; } = "";
    public DateTime CreatedAtUtc { get; private set; }
    private SavedRoute() { }
    public SavedRoute(string userId, string name, string routeJson)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        RouteJson = routeJson;
        CreatedAtUtc = DateTime.UtcNow;
    }
}