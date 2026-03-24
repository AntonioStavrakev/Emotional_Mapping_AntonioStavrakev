using System.ComponentModel.DataAnnotations;

namespace Emotional_Mapping.Web.Models;

public class RegisterViewModel
{
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "Полето е задължително")]
    [EmailAddress(ErrorMessage = "Невалиден имейл")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Полето е задължително")]
    [MinLength(6, ErrorMessage = "Минимум 6 символа")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Полето е задължително")]
    [Compare("Password", ErrorMessage = "Паролите не съвпадат")]
    public string ConfirmPassword { get; set; } = "";
}