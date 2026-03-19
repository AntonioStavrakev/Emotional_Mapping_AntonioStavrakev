using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<EmotionCatalogItem> EmotionCatalog => Set<EmotionCatalogItem>();
    public DbSet<EmotionalPoint> EmotionalPoints => Set<EmotionalPoint>();
    public DbSet<MapRequest> MapRequests => Set<MapRequest>();
    public DbSet<GeneratedMap> GeneratedMaps => Set<GeneratedMap>();
    public DbSet<MapRecommendation> MapRecommendations => Set<MapRecommendation>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<SavedRoute> SavedRoutes => Set<SavedRoute>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}