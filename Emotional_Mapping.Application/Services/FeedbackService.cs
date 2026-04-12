using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Services;

public class FeedbackService
{
    private readonly IFeedbackRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public FeedbackService(IFeedbackRepository repo, IUnitOfWork uow, ICurrentUser user)
    {
        _repo = repo;
        _uow = uow;
        _user = user;
    }

    public async Task AddAsync(FeedbackDto dto, CancellationToken ct)
    {
        var userId = _user.IsAuthenticated && !string.IsNullOrWhiteSpace(_user.UserId)
            ? _user.UserId!
            : "guest";

        var fb = new Feedback(
            userId: userId,
            generatedMapId: dto.GeneratedMapId,
            recommendationId: dto.RecommendationId,
            rating: dto.Rating,
            reaction: dto.Reaction,
            comment: dto.Comment
        );

        await _repo.AddAsync(fb, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
