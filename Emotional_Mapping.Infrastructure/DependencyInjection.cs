using System.Net.Http.Headers;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Application.Mapping;
using Emotional_Mapping.Application.Services;
using Emotional_Mapping.Infrastructure.AI;
using Emotional_Mapping.Infrastructure.Data;
using Emotional_Mapping.Infrastructure.Identity;
using Emotional_Mapping.Infrastructure.Maps;
using Emotional_Mapping.Infrastructure.Payments;
using Emotional_Mapping.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEmotionalMappingInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Липсва ConnectionStrings:Default");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(cs));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        // AutoMapper
        services.AddAutoMapper(am => am.AddProfile<MappingProfile>());

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Emotional_Mapping.Application.Validation.GenerateMapRequestDtoValidator>();

        // Repositories
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IEmotionalPointRepository, EmotionalPointRepository>();
        services.AddScoped<IMapRepository, MapRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IHeatmapService, HeatmapService>();
        
        services.AddScoped<ISavedRouteRepository, SavedRouteRepository>();
        // ==== OpenAI (HttpClient + Options) ====
        services.Configure<OpenAiOptions>(cfg.GetSection("OpenAI"));

        services.AddHttpClient("openai", (sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;

            http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opt.ApiKey);
        });

        // IAiEmotionService избор:
        // ако имаш OpenAI ключ -> OpenAiEmotionService
        // иначе -> RuleBasedEmotionAnalysisService
        var openAiKey = cfg["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(openAiKey))
            services.AddScoped<IAiEmotionService, OpenAiEmotionService>();
        else
            services.AddScoped<IAiEmotionService, RuleBasedEmotionAnalysisService>();

        // Application services
        services.AddScoped<MapGenerationService>();
        services.AddScoped<EmotionalPointsService>();
        services.AddScoped<FeedbackService>();
        services.AddScoped<ReportService>();
        services.AddScoped<StatsService>();

        services.AddScoped<IDistrictRepository, DistrictRepository>();
        return services;
    }
}