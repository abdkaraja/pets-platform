using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PetPlatform.Infrastructure.Identity;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        // Seed roles
        string[] roles = { "Admin", "Customer", "Vet", "ServiceProvider" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Seed permission claims onto Admin role
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole != null)
        {
            var permissions = new[] { "Users.View", "Users.Manage", "Roles.Create", "Roles.Assign", "Pets.Manage", "Vets.View", "Vets.Manage", "Records.View" };
            foreach (var perm in permissions)
            {
                var claims = await roleManager.GetClaimsAsync(adminRole);
                if (!claims.Any(c => c.Type == "Permission" && c.Value == perm))
                {
                    await roleManager.AddClaimAsync(adminRole, new Claim("Permission", perm));
                    logger.LogInformation("Added permission {Permission} to Admin role", perm);
                }
            }
        }

        // Seed admin user
        var adminEmail = "admin@petplatform.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    logger.LogError("Failed to create admin user: {Error}", error.Description);
                }
            }
        }
    }
}
