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
    private readonly ICityRepository _cities;
    private readonly IPlaceRepository _places;

    public EmotionalPointsService(
        IEmotionalPointRepository repo,
        IUnitOfWork uow,
        ICurrentUser user,
        ICityRepository cities,
        IPlaceRepository places)
    {
        _repo = repo;
        _uow = uow;
        _user = user;
        _cities = cities;
        _places = places;
    }

    public async Task<Guid> AddAsync(AddEmotionalPointDto dto, CancellationToken ct)
    {
        if (!_user.IsAuthenticated || string.IsNullOrWhiteSpace(_user.UserId))
        {
            throw new InvalidOperationException("Трябва да си влязъл в профила си.");
        }

        _ = await _cities.GetAsync(dto.CityId, ct)
            ?? throw new InvalidOperationException("Невалиден град.");

        Place? place = null;
        if (dto.PlaceId.HasValue)
        {
            place = await _places.GetAsync(dto.PlaceId.Value, ct)
                ?? throw new InvalidOperationException("Избраното място не е намерено.");

            if (place.CityId != dto.CityId)
                throw new InvalidOperationException("Избраното място не принадлежи към този град.");
        }

        var point = new EmotionalPoint(
            userId: _user.UserId!,
            cityId: dto.CityId,
            location: new GeoPoint(dto.Lat, dto.Lng),
            emotion: dto.Emotion ?? throw new InvalidOperationException("Моля, избери емоция."),
            intensity: dto.Intensity,
            placeId: dto.PlaceId,
            title: NormalizeOptional(dto.Title),
            note: NormalizeOptional(dto.Note),
            timeOfDay: NormalizeOptional(dto.TimeOfDay),
            isAnonymous: dto.IsAnonymous,
            isApproved: _user.IsInRole("Admin")
        );

        point.DistrictId = place?.DistrictId;

        if (!point.DistrictId.HasValue)
        {
            var cityPlaces = await _places.GetByCityAsync(dto.CityId, null, null, ct);
            var nearestDistrictPlace = cityPlaces
                .Where(x => x.DistrictId.HasValue)
                .Select(x => new
                {
                    Place = x,
                    DistanceMeters = DistanceMeters(dto.Lat, dto.Lng, x.Location.Lat, x.Location.Lng)
                })
                .Where(x => x.DistanceMeters <= 4000)
                .OrderBy(x => x.DistanceMeters)
                .FirstOrDefault();

            point.DistrictId = nearestDistrictPlace?.Place.DistrictId;
        }

        await _repo.AddAsync(point, ct);
        await _uow.SaveChangesAsync(ct);
        return point.Id;
    }

    public async Task ApproveAsync(Guid id, CancellationToken ct)
    {
        if (!_user.IsAuthenticated || !_user.IsInRole("Admin"))
            throw new InvalidOperationException("Нямаш право да одобряваш точки.");

        var point = await _repo.GetAsync(id, ct)
                    ?? throw new InvalidOperationException("Точката не е намерена.");

        point.Approve();
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var point = await _repo.GetAsync(id, ct)
                    ?? throw new InvalidOperationException("Точката не е намерена.");

        if (_user.UserId != point.UserId && !_user.IsInRole("Admin"))
        {
            throw new InvalidOperationException("Нямаш право да изтриеш тази точка.");
        }

        await _repo.DeleteAsync(point, ct);
        await _uow.SaveChangesAsync(ct);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6371000;

        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return 2 * earthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
