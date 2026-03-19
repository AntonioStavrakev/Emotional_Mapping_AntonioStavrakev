using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Services;

public class ReportService
{
    private readonly IReportRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public ReportService(IReportRepository repo, IUnitOfWork uow, ICurrentUser user)
    {
        _repo = repo;
        _uow = uow;
        _user = user;
    }

    public async Task AddAsync(ReportDto dto, CancellationToken ct)
    {
        if (!_user.IsAuthenticated || string.IsNullOrWhiteSpace(_user.UserId))
            throw new InvalidOperationException("Трябва да си влязъл в профила си.");

        var report = new Report(
            reporterUserId: _user.UserId!,
            reason: dto.Reason,
            emotionalPointId: dto.EmotionalPointId,
            placeId: dto.PlaceId
        );

        await _repo.AddAsync(report, ct);
        await _uow.SaveChangesAsync(ct);
    }
}