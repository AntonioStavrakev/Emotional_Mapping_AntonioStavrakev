using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.ValueObjects;

namespace Emotional_Mapping.Application.Services;

public class PlaceSuggestionService
{
    private readonly ICityRepository _cities;
    private readonly IDistrictRepository _districts;
    private readonly IPlaceRepository _places;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public PlaceSuggestionService(
        ICityRepository cities,
        IDistrictRepository districts,
        IPlaceRepository places,
        IUnitOfWork uow,
        ICurrentUser currentUser)
    {
        _cities = cities;
        _districts = districts;
        _places = places;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> SuggestAsync(SuggestPlaceDto dto, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new InvalidOperationException("Трябва да си влязъл в профила си, за да предложиш място.");

        _ = await _cities.GetAsync(dto.CityId, ct)
            ?? throw new InvalidOperationException("Невалиден град.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Въведи име на мястото.");

        if (dto.Lat is < -90 or > 90 || dto.Lng is < -180 or > 180)
            throw new InvalidOperationException("Избери валидна позиция на картата.");

        if (dto.DistrictId.HasValue)
        {
            var districts = await _districts.GetByCityAsync(dto.CityId, ct);
            if (districts.All(x => x.Id != dto.DistrictId.Value))
                throw new InvalidOperationException("Избраният район не принадлежи към този град.");
        }

        var place = new Place(
            cityId: dto.CityId,
            name: dto.Name,
            type: dto.Type,
            location: new GeoPoint(dto.Lat, dto.Lng),
            districtId: dto.DistrictId,
            description: NormalizeOptional(dto.Description),
            address: NormalizeOptional(dto.Address),
            source: "user-suggested",
            isApproved: false);

        await _places.AddAsync(place, ct);
        await _uow.SaveChangesAsync(ct);

        return place.Id;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
