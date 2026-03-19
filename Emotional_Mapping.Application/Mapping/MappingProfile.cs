using AutoMapper;
using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Domain.Entities;

namespace Emotional_Mapping.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<MapRecommendation, RecommendationDto>()
            .ForMember(d => d.PlaceId, o => o.MapFrom(s => s.Place.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Place.Name))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Place.Type))
            .ForMember(d => d.Lat, o => o.MapFrom(s => s.Place.Location.Lat))
            .ForMember(d => d.Lng, o => o.MapFrom(s => s.Place.Location.Lng))
            .ForMember(d => d.Emotion, o => o.MapFrom(s => s.Emotion))
            .ForMember(d => d.Score, o => o.MapFrom(s => s.Score))
            .ForMember(d => d.Reason, o => o.MapFrom(s => s.Reason));
    }
    
}