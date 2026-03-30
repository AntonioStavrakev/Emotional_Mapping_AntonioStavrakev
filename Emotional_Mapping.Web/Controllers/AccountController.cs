using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Emotional_Mapping.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _configuration;

    public AccountController(IHttpClientFactory http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _http.CreateClient("api");

        var payload = JsonSerializer.Serialize(new
        {
            email = model.Email,
            password = model.Password
        });

        var response = await client.PostAsync(
            "/api/auth/login",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            
            ViewBag.Error = string.IsNullOrWhiteSpace(raw)
                ? "Невалиден имейл или парола."
                : raw;
            return View(model);
        }
        
        string displayName = model.Email;
        List<string> roles = new() { "User" };

        try
        {
            using var doc = JsonDocument.Parse(raw);

            if (doc.RootElement.TryGetProperty("displayName", out var dn))
                displayName = dn.GetString() ?? model.Email;

            if (doc.RootElement.TryGetProperty("roles", out var rs) &&
                rs.ValueKind == JsonValueKind.Array)
            {
                roles = rs.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>()
                    .ToList();
            }
        }
        catch
        {
            // ignore parse errors
        }

        await SignInWebCookie(model.Email, displayName, roles);
        // await SignInWebCookie(model.Email, model.Email, new List<string> { "User" });

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _http.CreateClient("api");

        var payload = JsonSerializer.Serialize(new
        {
            email = model.Email,
            password = model.Password,
            displayName = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Email : model.DisplayName
        });

        var response = await client.PostAsync(
            "/api/auth/register",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync();
            ViewBag.Error = ExtractApiError(raw);
            return View(model);
        }

        await SignInWebCookie(
            model.Email,
            string.IsNullOrWhiteSpace(model.DisplayName) ? model.Email : model.DisplayName!,
            new List<string> { "User" });

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ===== GOOGLE LOGIN =====

    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new
        {
            returnUrl = returnUrl ?? "/"
        });

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (result?.Principal?.Identity?.IsAuthenticated == true)
        {
            // already signed in somehow
            return LocalRedirect(returnUrl ?? "/");
        }

        var externalResult = await HttpContext.AuthenticateAsync("External");
        if (!externalResult.Succeeded || externalResult.Principal == null)
        {
            TempData["Error"] = "Неуспешен Google login.";
            return RedirectToAction(nameof(Login));
        }

        var email = externalResult.Principal.FindFirstValue(ClaimTypes.Email);
        var name =
            externalResult.Principal.FindFirstValue(ClaimTypes.Name) ??
            email ??
            "Потребител";

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Google не върна имейл адрес.";
            return RedirectToAction(nameof(Login));
        }

        await EnsureUserExistsInApi(email, name);
        await SignInWebCookie(email, name, new List<string> { "User" });

        await HttpContext.SignOutAsync("External");
        return LocalRedirect(returnUrl ?? "/");
    }

    // ===== APPLE LOGIN =====

    [HttpGet]
    public IActionResult AppleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(AppleCallback), "Account", new
        {
            returnUrl = returnUrl ?? "/"
        });

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, "Apple");
    }

    [HttpGet]
    public async Task<IActionResult> AppleCallback(string? returnUrl = "/")
    {
        var externalResult = await HttpContext.AuthenticateAsync("External");
        if (!externalResult.Succeeded || externalResult.Principal == null)
        {
            TempData["Error"] = "Неуспешен Apple login.";
            return RedirectToAction(nameof(Login));
        }

        var email = externalResult.Principal.FindFirstValue(ClaimTypes.Email);
        var name =
            externalResult.Principal.FindFirstValue(ClaimTypes.Name) ??
            email ??
            "Потребител";

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Apple не върна имейл адрес. Това често става, ако при Apple акаунта е скрит имейлът.";
            return RedirectToAction(nameof(Login));
        }

        await EnsureUserExistsInApi(email, name);
        await SignInWebCookie(email, name, new List<string> { "User" });

        await HttpContext.SignOutAsync("External");
        return LocalRedirect(returnUrl ?? "/");
    }

    // ===== helpers =====

    private async Task EnsureUserExistsInApi(string email, string displayName)
    {
        var client = _http.CreateClient("api");

        // регистрираме потребителя тихо, ако не съществува
        var randomPassword = "Ext!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));

        var payload = JsonSerializer.Serialize(new
        {
            email,
            password = randomPassword,
            displayName
        });

        var response = await client.PostAsync(
            "/api/auth/register",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        // Ако user вече съществува, просто продължаваме.
        // Не хвърляме грешка нарочно.
        _ = response;
    }

    private async Task SignInWebCookie(string email, string displayName, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };

        foreach (var role in roles.Distinct())
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }

    private static string ExtractApiError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Грешка при регистрация.";

        try
        {
            using var doc = JsonDocument.Parse(raw);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var messages = new List<string>();

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("description", out var desc))
                        messages.Add(desc.GetString() ?? "");
                }

                var combined = string.Join(" ", messages.Where(x => !string.IsNullOrWhiteSpace(x)));
                return string.IsNullOrWhiteSpace(combined) ? "Грешка при регистрация." : combined;
            }
        }
        catch
        {
            // ignore parse error
        }

        return raw;
    }
}