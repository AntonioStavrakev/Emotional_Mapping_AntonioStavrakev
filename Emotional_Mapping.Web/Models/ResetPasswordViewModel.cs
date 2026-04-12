using System.ComponentModel.DataAnnotations;

namespace Emotional_Mapping.Web.Models;

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";

    [Required(ErrorMessage = "Новата парола е задължителна.")]
    [MinLength(6, ErrorMessage = "Паролата трябва да е поне 6 символа.")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Потвърдете паролата.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = "";
}
