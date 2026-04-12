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
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$",
        ErrorMessage = "Паролата трябва да има поне 6 символа, 1 главна буква, 1 цифра и 1 специален символ")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Полето е задължително")]
    [Compare("Password", ErrorMessage = "Паролите не съвпадат")]
    public string ConfirmPassword { get; set; } = "";
}
