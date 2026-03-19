using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Application.Services;

public class EmotionalPointsService
{
    private readonly IEmotionalPointRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public EmotionalPointsService(IEmotionalPointRepository repo, IUnitOfWork uow, ICurrentUser user)
    {
        _repo = repo;
        _uow = uow;
        _user = user;
    }

    public async Task<Guid> AddAsync(AddEmotionalPointDto dto, CancellationToken ct)
    {
        if (!_user.IsAuthenticated || string.IsNullOrWhiteSpace(_user.UserId))
        {
            throw new InvalidOperationException("Трябва да си влязъл в профила си.");
        }

        var point = new EmotionalPoint(
            userId: _user.UserId!,
            cityId: dto.CityId,
            location: new GeoPoint(dto.Lat, dto.Lng),
            emotion: dto.Emotion,
            intensity: dto.Intensity,
            placeId: dto.PlaceId,
            title: dto.Title,
            note: dto.Note,
            timeOfDay: dto.TimeOfDay,
            isAnonymous: dto.IsAnonymous,
            isApproved: true
        );

        await _repo.AddAsync(point, ct);
        await _uow.SaveChangesAsync(ct);
        return point.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var point = await _repo.GetAsync(id, ct)
                    ?? throw new InvalidOperationException("Точката не е намерена.");

        if (_user.UserId != point.UserId && !_user.IsInRole("Admin") && !_user.IsInRole("Moderator"))
        {
            throw new InvalidOperationException("Нямаш право да изтриеш тази точка.");
        }

        await _repo.DeleteAsync(point, ct);
        await _uow.SaveChangesAsync(ct);
    }
}