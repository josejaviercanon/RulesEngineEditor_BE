using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace RulesEngineEditor.Server.Infrastructure.Identity;

public static class DatabaseSeeder
{
    public static async Task SeedAdminUserAsync(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<Program> logger)
    {
        string roleName = "Administrator";
        string adminEmail = "admin@localhost.local";
        const string adminPassword = "Admin@123456"; // Strong password meeting default requirements

        // 1. Ensure the Administrator role exists
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Administrator role created successfully.");
            }
            else
            {
                logger.LogError("Failed to create Administrator role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}")));
            }
        }

        // 2. Ensure the Local Admin user exists
        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser != null)
        {
            logger.LogInformation("Admin user with email {Email} already exists.", adminEmail);
            return;
        }

        var adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        // Creates user and hashes password natively using current security defaults
        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            logger.LogInformation("Admin user created successfully with email {Email}.", adminEmail);

            // Assign the local user to the local admin role
            var roleAssignResult = await userManager.AddToRoleAsync(adminUser, roleName);
            if (roleAssignResult.Succeeded)
            {
                logger.LogInformation("Admin user assigned to Administrator role.");
            }
            else
            {
                logger.LogError("Failed to assign admin user to role: {Errors}",
                    string.Join(", ", roleAssignResult.Errors.Select(e => $"{e.Code}: {e.Description}")));
            }
        }
        else
        {
            logger.LogError("Failed to create admin user. Errors: {Errors}",
                string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
        }
    }
}
