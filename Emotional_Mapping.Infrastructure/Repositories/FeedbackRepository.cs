using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Data;

namespace Emotional_Mapping.Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _db;

    public FeedbackRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(Feedback feedback, CancellationToken ct)
    {
        return _db.Feedbacks.AddAsync(feedback, ct).AsTask();
    }
}