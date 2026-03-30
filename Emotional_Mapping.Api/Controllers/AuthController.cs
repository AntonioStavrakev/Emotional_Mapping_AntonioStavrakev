using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Имейлът е задължителен.");

        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Паролата е задължителна.");

        var existing = await _users.FindByEmailAsync(req.Email);
        if (existing != null)
            return BadRequest("Вече има профил с този имейл.");

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? "Потребител" : req.DisplayName,
            EmailConfirmed = true
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(x => x.Description).ToList();
            return BadRequest(errors);
        }

        await _users.AddToRoleAsync(user, "User");

        return Ok(new
        {
            message = "Регистрация успешна."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null)
            return Unauthorized("Невалиден имейл или парола.");

        var result = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized("Невалиден имейл или парола.");

        var roles = await _users.GetRolesAsync(user);

        return Ok(new
        {
            message = "Успешен вход.",
            email = user.Email,
            displayName = user.DisplayName,
            roles = roles
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Изход успешен." });
    }

    public sealed class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? DisplayName { get; set; }
    }

    public sealed class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}