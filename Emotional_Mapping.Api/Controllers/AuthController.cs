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
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? "Потребител" : req.DisplayName,
            EmailConfirmed = true
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _users.AddToRoleAsync(user, "User");
        return Ok(new { message = "Регистрация успешна." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _signIn.PasswordSignInAsync(req.Email, req.Password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Невалиден имейл или парола." });

        return Ok(new { message = "Успешен вход." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
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