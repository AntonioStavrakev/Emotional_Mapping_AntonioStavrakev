using System.Security.Claims;
using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Emotional_Mapping.Api.Middleware;

/// <summary>
/// When requests come from the Web proxy (not directly from the browser),
/// this middleware reads the X-User-Email header and authenticates the user
/// using Identity's UserManager. This way the API's [Authorize] attributes work
/// for both direct API calls and proxied requests from the Web frontend.
/// </summary>
public class ProxyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ProxyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process if not already authenticated
        if (context.User.Identity?.IsAuthenticated != true &&
            context.Request.Headers.TryGetValue("X-User-Email", out var emailHeader))
        {
            var email = emailHeader.ToString();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByEmailAsync(email);

                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id),
                        new(ClaimTypes.Email, user.Email ?? email),
                        new(ClaimTypes.Name, user.DisplayName ?? email)
                    };

                    foreach (var role in roles)
                        claims.Add(new Claim(ClaimTypes.Role, role));

                    var identity = new ClaimsIdentity(claims, "ProxyAuth");
                    context.User = new ClaimsPrincipal(identity);
                }
            }
        }

        await _next(context);
    }
}

public static class ProxyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseProxyAuth(this IApplicationBuilder builder)
        => builder.UseMiddleware<ProxyAuthMiddleware>();
}
