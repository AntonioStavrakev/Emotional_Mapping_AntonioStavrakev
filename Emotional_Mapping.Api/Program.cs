using Emotional_Mapping.Api.Payments;
using Emotional_Mapping.Application.Validation;
using Emotional_Mapping.Infrastructure.Data;
using Emotional_Mapping.Infrastructure.Data.Seed;
using FluentValidation.AspNetCore;
using Emotional_Mapping.Infrastructure;
using Emotional_Mapping.Infrastructure.AI;
using Emotional_Mapping.Infrastructure.Identity;
using Emotional_Mapping.Infrastructure.OpenAiServices;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

using System.Text.Json.Serialization;
using Emotional_Mapping.Api.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<GenerateMapRequestDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<OpenAiService>();
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));

builder.Services.AddEmotionalMappingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseProxyAuth(); // Process X-User-Email from Web proxy
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    var db = sp.GetRequiredService<AppDbContext>();

    // First: apply migrations
    await db.Database.MigrateAsync(CancellationToken.None);
    await EnsureSchemaConsistencyAsync(db);

    // Second: seed cities (BgCitiesSeeder checks if cities exist)
    await BgCitiesSeeder.SeedAsync(db);

    // Third: seed districts before places so seed data can attach district IDs
    await BgDistrictsSeeder.SeedAsync(db, CancellationToken.None);

    // Fourth: seed places/emotion catalog and backfill district assignments
    await DbSeeder.SeedAsync(db, CancellationToken.None);

    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    await RoleSeeder.SeedAsync(roleManager, userManager);
}

app.Run();

static async Task EnsureSchemaConsistencyAsync(AppDbContext db)
{
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE "EmotionalPoints"
        ALTER COLUMN "DistrictId" DROP NOT NULL;
        """);

    await db.Database.ExecuteSqlRawAsync("""
        UPDATE "EmotionalPoints"
        SET "DistrictId" = NULL
        WHERE "DistrictId" = '00000000-0000-0000-0000-000000000000';
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_MapRequests_CreatedAtUtc"
        ON "MapRequests" ("CreatedAtUtc");
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_MapRequests_CityId_CreatedAtUtc"
        ON "MapRequests" ("CityId", "CreatedAtUtc");
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_EmotionalPoints_CityId_IsApproved"
        ON "EmotionalPoints" ("CityId", "IsApproved");
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_Places_CityId_DistrictId_IsApproved"
        ON "Places" ("CityId", "DistrictId", "IsApproved");
        """);
}
