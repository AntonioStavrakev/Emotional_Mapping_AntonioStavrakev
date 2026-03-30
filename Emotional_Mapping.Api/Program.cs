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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    var db = sp.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db, CancellationToken.None);

    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    await RoleSeeder.SeedAsync(roleManager, userManager);
}

app.Run();