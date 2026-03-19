using Microsoft.AspNetCore.Identity;

namespace Emotional_Mapping.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "Потребител";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}