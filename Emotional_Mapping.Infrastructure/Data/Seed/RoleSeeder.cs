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

        const string adminEmail = "admin@geofeel.tech";
        const string legacyAdminEmail = "admin@geofeel.bg";
        const string adminPass = "Admin123!";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            var legacyAdmin = await userManager.FindByEmailAsync(legacyAdminEmail);
            if (legacyAdmin != null)
            {
                if (!string.Equals(legacyAdmin.UserName, adminEmail, StringComparison.OrdinalIgnoreCase))
                    await EnsureSuccessAsync(await userManager.SetUserNameAsync(legacyAdmin, adminEmail));

                if (!string.Equals(legacyAdmin.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
                    await EnsureSuccessAsync(await userManager.SetEmailAsync(legacyAdmin, adminEmail));

                legacyAdmin.EmailConfirmed = true;
                await EnsureSuccessAsync(await userManager.UpdateAsync(legacyAdmin));
                admin = legacyAdmin;
            }
        }

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

    private static Task EnsureSuccessAsync(IdentityResult result)
    {
        if (result.Succeeded)
            return Task.CompletedTask;

        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Role seeding failed: {errors}");
    }
}
