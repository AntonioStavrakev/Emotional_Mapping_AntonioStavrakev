using System.ComponentModel.DataAnnotations;

namespace Emotional_Mapping.Web.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Имейлът е задължителен.")]
    [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
    public string Email { get; set; } = "";
}
