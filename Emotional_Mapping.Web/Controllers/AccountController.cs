using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Emotional_Mapping.Infrastructure.Identity;
using Emotional_Mapping.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _http;

    public AccountController(IHttpClientFactory http)
    {
        _http = http;
    }

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _http.CreateClient("api");

        var payload = JsonSerializer.Serialize(new
        {
            email    = model.Email,
            password = model.Password
        });

        var response = await client.PostAsync(
            "/api/auth/login",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Невалиден имейл или парола.";
            return View(model);
        }

        // Прочитаме ролите от API-то
        var rolesResponse = await client.GetAsync("/api/me/roles");
        var roles = new List<string>();

        if (rolesResponse.IsSuccessStatusCode)
        {
            var json    = await rolesResponse.Content.ReadAsStringAsync();
            var parsed  = JsonSerializer.Deserialize<List<string>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            roles = parsed ?? new List<string>();
        }

        // Създаваме Claims за Web cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name,  model.Email),
            new Claim(ClaimTypes.Email, model.Email)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    // POST /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _http.CreateClient("api");

        var payload = JsonSerializer.Serialize(new
        {
            email       = model.Email,
            password    = model.Password,
            displayName = model.DisplayName
        });

        var response = await client.PostAsync(
            "/api/auth/register",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            ViewBag.Error = "Грешка при регистрация. Опитай с друг имейл.";
            return View(model);
        }

        // Автоматичен вход след регистрация
        var loginModel = new LoginViewModel
        {
            Email    = model.Email,
            Password = model.Password
        };

        return await Login(loginModel);
    }

    // GET /Account/Logout — показва страницата, тя прави POST автоматично
    [HttpGet]
    public IActionResult Logout()
    {
        return View();
    }

    // POST /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        // Излизаме от API-то
        var client = _http.CreateClient("api");
        await client.PostAsync("/api/auth/logout", null);

        // Изтриваме Web cookie-то
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }
}