using System.ComponentModel.DataAnnotations;

namespace Emotional_Mapping.Web.Models;

public class ContactViewModel
{
    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имейлът е задължителен.")]
    [EmailAddress(ErrorMessage = "Невалиден имейл.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Съобщението е задължително.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}