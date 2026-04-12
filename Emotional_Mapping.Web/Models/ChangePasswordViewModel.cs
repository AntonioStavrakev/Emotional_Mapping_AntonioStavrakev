using System.ComponentModel.DataAnnotations;

namespace Emotional_Mapping.Web.Models;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Текущата парола е задължителна.")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Новата парола е задължителна.")]
    [MinLength(6, ErrorMessage = "Паролата трябва да е поне 6 символа.")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Потвърдете паролата.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = "";
}
