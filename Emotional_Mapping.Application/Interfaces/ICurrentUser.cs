namespace Emotional_Mapping.Application.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}