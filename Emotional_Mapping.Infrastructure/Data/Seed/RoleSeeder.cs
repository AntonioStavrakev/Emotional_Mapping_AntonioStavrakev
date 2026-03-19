using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Emotional_Mapping.Infrastructure.Data.Seed;

public static class RoleSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        string[] roles = { "Admin", "SuperUser", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        const string adminEmail = "admin@geofeel.bg";
        const string adminPass = "Admin123!";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Администратор"
            };

            var created = await userManager.CreateAsync(admin, adminPass);
            if (created.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}