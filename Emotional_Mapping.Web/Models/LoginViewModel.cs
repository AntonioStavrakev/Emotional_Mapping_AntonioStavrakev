using System.ComponentModel.DataAnnotations;

namespace Emotional_Mapping.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Полето е задължително")]
    [EmailAddress(ErrorMessage = "Невалиден имейл")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Полето е задължително")]
    public string Password { get; set; } = "";
}