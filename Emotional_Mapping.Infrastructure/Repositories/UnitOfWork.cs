using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }

}