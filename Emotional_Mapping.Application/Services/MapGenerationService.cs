using AutoMapper;
using Emotional_Mapping.Application.AI;
using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Application.Services;

public class MapGenerationService
{
    private readonly ICityRepository _cities;
    private readonly IPlaceRepository _places;
    private readonly IEmotionalPointRepository _points;
    private readonly IMapRepository _maps;
    private readonly IAiEmotionService _ai;
    private readonly IHeatmapService _heatmap;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public MapGenerationService(
        ICityRepository cities,
        IPlaceRepository places,
        IEmotionalPointRepository points,
        IMapRepository maps,
        IAiEmotionService ai,
        IHeatmapService heatmap,
        IUnitOfWork uow,
        IMapper mapper,
        ICurrentUser currentUser)
    {
        _cities = cities;
        _places = places;
        _points = points;
        _maps = maps;
        _ai = ai;
        _heatmap = heatmap;
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<GenerateMapResultDto> GenerateAsync(GenerateMapRequestDto dto, CancellationToken ct)
    {
        if (dto.CityId == Guid.Empty)
            throw new InvalidOperationException("Избери град.");

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new InvalidOperationException("Трябва да си влязъл в профила си.");

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);

        var requestsToday = await _maps.CountRequestsForUserTodayAsync(
            _currentUser.UserId!,
            todayStart,
            todayEnd,
            ct);

        var limit = _currentUser.IsInRole("SuperUser") || _currentUser.IsInRole("Admin") ? 100 : 10;
        if (requestsToday >= limit)
            throw new InvalidOperationException($"Достигнат е дневният лимит от {limit} AI заявки.");

        var city = await _cities.GetAsync(dto.CityId, ct)
                   ?? throw new InvalidOperationException("Невалиден град.");

        var places = await _places.GetByCityAsync(dto.CityId, dto.DistrictId, dto.SelectedPlaceType, ct);
        var points = await _points.GetByCityAsync(dto.CityId, dto.SelectedEmotion, ct);

        var aiInput = new AiAnalysisInput
        {
            CityName = city.Name,
            QueryText = dto.QueryText ?? "",
            EmotionHint = dto.SelectedEmotion,
            PlaceTypeHint = dto.SelectedPlaceType,
            RadiusMeters = dto.RadiusMeters,
            Places = places.Select(p => new PlaceContextItem
            {
                PlaceId = p.Id,
                Name = p.Name,
                Type = p.Type,
                Lat = p.Location.Lat,
                Lng = p.Location.Lng,
                Description = p.Description
            }).ToList(),
            EmotionalSignals = points.Select(p => new EmotionalSignalItem
            {
                Lat = p.Location.Lat,
                Lng = p.Location.Lng,
                Emotion = p.Emotion,
                Intensity = p.Intensity
            }).ToList()
        };

        var aiResult = await _ai.AnalyzeAsync(aiInput, ct);

        var req = new MapRequest(
            cityId: dto.CityId,
            queryText: dto.QueryText ?? "",
            userId: _currentUser.UserId,
            districtId: dto.DistrictId,
            selectedEmotion: dto.SelectedEmotion,
            selectedPlaceType: dto.SelectedPlaceType,
            language: "bg",
            radiusMeters: dto.RadiusMeters
        );

        req.SetAiInfo(aiResult.AiModel, aiResult.TokensUsed);

        await _maps.AddRequestAsync(req, ct);

        var gen = new GeneratedMap(
            mapRequestId: req.Id,
            title: $"Емоционална карта: {city.Name}",
            dominantEmotion: aiResult.FinalEmotion,
            confidence: aiResult.Confidence,
            visibility: MapVisibility.Private,
            summary: aiResult.Summary
        );

        foreach (var rec in aiResult.Recommendations)
        {
            var place = places.FirstOrDefault(p => p.Id == rec.PlaceId);
            if (place is null) continue;

            var entity = new MapRecommendation(
                generatedMapId: gen.Id,
                placeId: place.Id,
                emotion: aiResult.FinalEmotion,
                score: rec.Score,
                reason: rec.Reason
            );

            gen.AddRecommendation(entity);
        }

        gen.SetHeatmap(_heatmap.BuildHeatmapJson(points, gen.Recommendations));

        await _maps.AddGeneratedMapAsync(gen, ct);
        await _uow.SaveChangesAsync(ct);

        return new GenerateMapResultDto
        {
            Title = gen.Title,
            DominantEmotion = gen.DominantEmotion,
            Summary = gen.Summary,
            HeatmapJson = gen.HeatmapJson,
            Recommendations = _mapper.Map<List<RecommendationDto>>(gen.Recommendations)
        };
    }
}