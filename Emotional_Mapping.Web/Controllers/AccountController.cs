using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Emotional_Mapping.Web.Models;
using Emotional_Mapping.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Emotional_Mapping.Web.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _configuration;
    private readonly IContactEmailService _emailService;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IUserOnboardingService _onboarding;

    public AccountController(
        IHttpClientFactory http,
        IConfiguration configuration,
        IContactEmailService emailService,
        IAuthenticationSchemeProvider schemes,
        IUserOnboardingService onboarding)
    {
        _http = http;
        _configuration = configuration;
        _emailService = emailService;
        _schemes = schemes;
        _onboarding = onboarding;
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
            ViewBag.Error = ExtractApiError(
                raw,
                "Невалиден имейл или парола.",
                response.StatusCode);
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
            ViewBag.Error = ExtractApiError(
                raw,
                "Грешка при регистрация.",
                response.StatusCode);
            return View(model);
        }

        await SignInWebCookie(
            model.Email,
            string.IsNullOrWhiteSpace(model.DisplayName) ? model.Email : model.DisplayName!,
            new List<string> { "User" });

        await _onboarding.HandleNewRegistrationAsync(
            model.Email,
            string.IsNullOrWhiteSpace(model.DisplayName) ? model.Email : model.DisplayName!);

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

    // ===== FORGOT PASSWORD =====

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _http.CreateClient("api");

        var payload = JsonSerializer.Serialize(new { email = model.Email });
        var response = await client.PostAsync(
            "/api/auth/forgot-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("token", out var tokenProp) &&
                    doc.RootElement.TryGetProperty("email", out var emailProp))
                {
                    var token = tokenProp.GetString() ?? "";
                    var email = emailProp.GetString() ?? model.Email;

                    // Build reset link
                    var resetLink = Url.Action("ResetPassword", "Account",
                        new { email = email, token = token },
                        protocol: Request.Scheme)!;

                    // Send email
                    try
                    {
                        var htmlBody = $@"
<div style=""font-family:'DM Sans',sans-serif;max-width:560px;margin:0 auto;padding:32px;background:#f6f8f3;border-radius:18px;"">
  <h1 style=""color:#4a8c1c;font-size:2rem;margin-bottom:8px;"">GEOFEEL</h1>
  <h2 style=""color:#233127;font-size:1.3rem;"">Смяна на парола</h2>
  <p style=""color:#66725f;"">Получихме заявка за смяна на паролата на твоя GEOFEEL профил.</p>
  <p style=""color:#66725f;"">Кликни върху бутона по-долу, за да зададеш нова парола. Линкът е валиден 24 часа.</p>
  <div style=""text-align:center;margin:32px 0;"">
    <a href=""{resetLink}"" style=""background:linear-gradient(90deg,#89d957,#c9e265);color:#233127;font-weight:700;padding:14px 32px;border-radius:12px;text-decoration:none;font-size:1rem;"">
      🔑 Смяна на парола
    </a>
  </div>
  <p style=""color:#8e9888;font-size:.85rem;"">Ако не си поискал(а) смяна на паролата, просто игнорирай този имейл.</p>
  <hr style=""border:none;border-top:1px solid #e8f0e0;margin:24px 0;""/>
  <p style=""color:#8e9888;font-size:.8rem;"">© 2026 GEOFEEL — Emotional Mapping</p>
</div>";

                        await _emailService.SendSystemEmailAsync(email, "Смяна на парола — GEOFEEL", htmlBody);
                    }
                    catch
                    {
                        // If email fails, still show a success message (don't leak info)
                    }
                }
            }
            catch { }
        }

        // Always show success to prevent email enumeration
        TempData["Success"] = "Ако имейлът е регистриран, ще получиш линк за смяна на парола.";
        return RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    // ===== RESET PASSWORD =====

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Невалиден линк за смяна на парола.";
            return RedirectToAction("Login");
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _http.CreateClient("api");

        var payload = JsonSerializer.Serialize(new
        {
            email = model.Email,
            token = model.Token,
            newPassword = model.NewPassword
        });

        var response = await client.PostAsync(
            "/api/auth/reset-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Паролата е сменена успешно. Можеш да влезеш с новата парола.";
            return RedirectToAction("Login");
        }

        var raw = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                ViewBag.Error = msg.GetString();
            else
                ViewBag.Error = "Грешка при смяна на паролата.";
        }
        catch
        {
            ViewBag.Error = "Грешка при смяна на паролата.";
        }

        return View(model);
    }

    // ===== CHANGE PASSWORD (logged in) =====

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _http.CreateClient("api");

        var payload = JsonSerializer.Serialize(new
        {
            currentPassword = model.CurrentPassword,
            newPassword = model.NewPassword
        });

        var response = await client.PostAsync(
            "/api/auth/change-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Паролата е сменена успешно.";
            return RedirectToAction("ChangePassword");
        }

        var raw = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                ViewBag.Error = msg.GetString();
            else
                ViewBag.Error = "Грешка при смяна на паролата.";
        }
        catch
        {
            ViewBag.Error = "Грешка при смяна на паролата.";
        }

        return View(model);
    }

    // ===== GOOGLE LOGIN =====

    [HttpGet]
    public async Task<IActionResult> GoogleLogin(string? returnUrl = null)
    {
        if (await _schemes.GetSchemeAsync("Google") is null)
        {
            TempData["Error"] = "Google входът още не е конфигуриран.";
            return RedirectToAction(nameof(Login));
        }

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
    public async Task<IActionResult> AppleLogin(string? returnUrl = null)
    {
        if (await _schemes.GetSchemeAsync("Apple") is null)
        {
            TempData["Error"] = "Apple входът още не е конфигуриран.";
            return RedirectToAction(nameof(Login));
        }

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

        if (response.IsSuccessStatusCode)
        {
            await _onboarding.HandleNewRegistrationAsync(email, displayName);
        }
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

    private static string ExtractApiError(string raw, string fallback, HttpStatusCode? statusCode = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return BuildFriendlyFallback(statusCode, fallback);

        try
        {
            using var doc = JsonDocument.Parse(raw);

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var messageProp))
            {
                var objectMessage = messageProp.GetString();
                if (!string.IsNullOrWhiteSpace(objectMessage))
                    return NormalizeApiError(objectMessage, fallback, statusCode);
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var messages = new List<string>();

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        messages.Add(item.GetString() ?? "");
                        continue;
                    }

                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("description", out var desc))
                    {
                        messages.Add(desc.GetString() ?? "");
                    }
                }

                var combined = string.Join(" ", messages.Where(x => !string.IsNullOrWhiteSpace(x)));
                return string.IsNullOrWhiteSpace(combined)
                    ? BuildFriendlyFallback(statusCode, fallback)
                    : NormalizeApiError(combined, fallback, statusCode);
            }
        }
        catch
        {
            // ignore parse error
        }

        return NormalizeApiError(raw, fallback, statusCode);
    }

    private static string NormalizeApiError(string raw, string fallback, HttpStatusCode? statusCode)
    {
        var decoded = WebUtility.HtmlDecode(raw).Trim();
        var singleLine = string.Join(" ", decoded
            .Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            .Trim();

        if (string.IsNullOrWhiteSpace(singleLine))
            return BuildFriendlyFallback(statusCode, fallback);

        if (statusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
            return "Сървърът временно не е наличен. Опитай отново след няколко секунди.";

        if (singleLine.Contains("error code: 502", StringComparison.OrdinalIgnoreCase) ||
            singleLine.Contains("bad gateway", StringComparison.OrdinalIgnoreCase))
        {
            return "Сървърът временно не е наличен. Опитай отново след няколко секунди.";
        }

        return singleLine;
    }

    private static string BuildFriendlyFallback(HttpStatusCode? statusCode, string fallback)
    {
        return statusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
            ? "Сървърът временно не е наличен. Опитай отново след няколко секунди."
            : fallback;
    }
}
