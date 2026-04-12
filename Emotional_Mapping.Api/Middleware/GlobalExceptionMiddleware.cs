using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Emotional_Mapping.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
            await WriteErrorAsync(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update failed");
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, MapDbUpdateMessage(ex));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe request failed");
            await WriteErrorAsync(context, HttpStatusCode.BadRequest,
                string.IsNullOrWhiteSpace(ex.Message)
                    ? "Възникна грешка при връзка със Stripe."
                    : ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError,
                "Възникна неочаквана грешка. Моля опитайте отново.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(new { message }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static string MapDbUpdateMessage(DbUpdateException ex)
    {
        var text = ex.ToString();

        if (text.Contains("\"DistrictId\"", StringComparison.OrdinalIgnoreCase) &&
            text.Contains("not-null constraint", StringComparison.OrdinalIgnoreCase))
        {
            return "Точката не можа да се запише, защото колоната за район в базата още не е обновена правилно. Рестартирай API-то, за да приложи поправката автоматично.";
        }

        if (text.Contains("foreign key", StringComparison.OrdinalIgnoreCase))
        {
            return "Подадените данни сочат към несъществуващ град, район или място.";
        }

        return "Възникна грешка при запис в базата данни.";
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        => builder.UseMiddleware<GlobalExceptionMiddleware>();
}
