namespace Emotional_Mapping.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}