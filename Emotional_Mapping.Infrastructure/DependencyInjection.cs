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
using Emotional_Mapping.Infrastructure.Places;
using Emotional_Mapping.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

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

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

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
        services.AddScoped<IAiCreditPackRepository, AiCreditPackRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IHeatmapService, HeatmapService>();
        services.AddScoped<IExternalPlaceProvider, GooglePlacesDiscoveryService>();
        services.AddScoped<IExternalPlaceProvider, FoursquarePlacesDiscoveryService>();
        services.AddScoped<IExternalPlaceProvider, OpenStreetMapPlaceDiscoveryService>();
        services.AddScoped<IExternalPlaceDiscoveryService, CompositeExternalPlaceDiscoveryService>();
        
        services.AddScoped<ISavedRouteRepository, SavedRouteRepository>();
        // ==== OpenAI (HttpClient + Options) ====
        services.Configure<OpenAiOptions>(cfg.GetSection("OpenAI"));
        services.Configure<ExternalPlaceDiscoveryOptions>(cfg.GetSection("ExternalPlaces"));
        services.Configure<OpenStreetMapOptions>(cfg.GetSection("ExternalPlaces"));
        services.Configure<GooglePlacesOptions>(cfg.GetSection("GooglePlaces"));
        services.Configure<FoursquarePlacesOptions>(cfg.GetSection("FoursquarePlaces"));

        services.AddHttpClient("openai", (sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            var apiKey = !string.IsNullOrWhiteSpace(opt.ApiKey)
                ? opt.ApiKey
                : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }
        });

        services.AddHttpClient("osm-overpass", (sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<OpenStreetMapOptions>>().Value;
            http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(opt.UserAgent))
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd(opt.UserAgent);
            }
        });

        services.AddHttpClient("google-places", (sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<GooglePlacesOptions>>().Value;
            http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddHttpClient("foursquare-places", (sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<FoursquarePlacesOptions>>().Value;
            http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(opt.ApiKey))
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", opt.ApiKey);
            }

            if (!string.IsNullOrWhiteSpace(opt.ApiVersion))
            {
                http.DefaultRequestHeaders.TryAddWithoutValidation("X-Places-Api-Version", opt.ApiVersion);
            }
        });

        // IAiEmotionService избор:
        // ако имаш OpenAI ключ -> OpenAiEmotionService
        // иначе -> RuleBasedEmotionAnalysisService
        var openAiKey = cfg["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(openAiKey))
            openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(openAiKey))
            services.AddScoped<IAiEmotionService, OpenAiEmotionService>();
        else
            services.AddScoped<IAiEmotionService, RuleBasedEmotionAnalysisService>();

        // Application services
        services.AddScoped<MapGenerationService>();
        services.AddScoped<EmotionalPointsService>();
        services.AddScoped<PlaceSuggestionService>();
        services.AddScoped<FeedbackService>();
        services.AddScoped<ReportService>();
        services.AddScoped<StatsService>();

        services.AddScoped<IDistrictRepository, DistrictRepository>();
        return services;
    }
}
