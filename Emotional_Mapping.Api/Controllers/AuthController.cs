using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Имейлът е задължителен." });

        var user = await _users.FindByEmailAsync(req.Email);

        // Always return OK to prevent email enumeration
        if (user == null)
            return Ok(new { message = "Ако имейлът съществува, ще получите линк за смяна на парола." });

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);

        return Ok(new
        {
            message = "Ако имейлът съществува, ще получите линк за смяна на парола.",
            token = encodedToken,
            email = user.Email
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Token) ||
            string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "Всички полета са задължителни." });

        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null)
            return BadRequest(new { message = "Невалидна заявка." });

        var decodedToken = Uri.UnescapeDataString(req.Token);
        var result = await _users.ResetPasswordAsync(user, decodedToken, req.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = string.Join(" ", errors) });
        }

        return Ok(new { message = "Паролата е сменена успешно." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "Всички полета са задължителни." });

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized();

        var user = await _users.FindByEmailAsync(email);
        if (user == null)
            return Unauthorized();

        var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = string.Join(" ", errors) });
        }

        return Ok(new { message = "Паролата е сменена успешно." });
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

    public sealed class ForgotPasswordRequest
    {
        public string Email { get; set; } = "";
    }

    public sealed class ResetPasswordRequest
    {
        public string Email { get; set; } = "";
        public string Token { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    public sealed class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}