using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emotional_Mapping.Web.Middleware;

public class ApiProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiProxyMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public ApiProxyMiddleware(
        RequestDelegate next,
        IHttpClientFactory httpClientFactory,
        ILogger<ApiProxyMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var client = _httpClientFactory.CreateClient("api");

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = new Uri(client.BaseAddress!, context.Request.Path + context.Request.QueryString)
        };

        // Forward only safe ASCII request headers. Some browser/extension headers may contain
        // non-ASCII values, which HttpClient rejects when writing outbound request headers.
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsSafeProxyHeader(header.Key, header.Value))
                continue;

            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Forward body for POST/PUT/PATCH
        if (context.Request.ContentLength > 0 ||
            context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
        {
            var stream = new MemoryStream();
            await context.Request.Body.CopyToAsync(stream);
            stream.Position = 0;
            requestMessage.Content = new StreamContent(stream);

            if (context.Request.ContentType != null)
                requestMessage.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);

            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!IsSafeProxyHeader(header.Key, header.Value))
                    continue;

                requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // Forward authenticated user info to the API via custom headers
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value);

            if (!string.IsNullOrEmpty(email))
                requestMessage.Headers.TryAddWithoutValidation("X-User-Email", email);
            if (!string.IsNullOrEmpty(name))
            {
                // Encode as Base64 to safely forward non-ASCII display names (e.g. Cyrillic)
                var nameB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(name));
                requestMessage.Headers.TryAddWithoutValidation("X-User-Name", nameB64);
            }
            if (roles.Any())
                requestMessage.Headers.TryAddWithoutValidation("X-User-Roles", string.Join(",", roles));
        }

        try
        {
            var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Remove transfer-encoding to avoid issues
            context.Response.Headers.Remove("transfer-encoding");

            await response.Content.CopyToAsync(context.Response.Body);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API proxy failed for {Path}. ApiBaseUrl={ApiBaseUrl}", context.Request.Path, client.BaseAddress);
            context.Response.StatusCode = 502;

            var isDevelopment = string.Equals(
                _configuration["ASPNETCORE_ENVIRONMENT"],
                "Development",
                StringComparison.OrdinalIgnoreCase);

            var message = isDevelopment
                ? $"API proxy error към {client.BaseAddress}: {ex.Message}"
                : "API сървърът не е наличен.";

            await context.Response.WriteAsJsonAsync(new { message });
        }
    }

    private static bool IsSafeProxyHeader(string key, Microsoft.Extensions.Primitives.StringValues values)
    {
        return IsAscii(key) && values.All(IsAscii);
    }

    private static bool IsAscii(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        foreach (var ch in value)
        {
            if (ch > sbyte.MaxValue)
                return false;
        }

        return true;
    }
}

public static class ApiProxyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiProxy(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiProxyMiddleware>();
    }
}
