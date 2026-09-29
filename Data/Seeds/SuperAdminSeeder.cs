using Microsoft.AspNetCore.Identity;
using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Entities.Authorization;

namespace Hotel_Management_System_Backend_dotNet.Data.Seeds;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var superAdminEmail = configuration["SuperAdmin:Email"];
        var superAdminPassword = configuration["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(superAdminEmail)
            || string.IsNullOrWhiteSpace(superAdminPassword))
        {
            throw new InvalidOperationException(
                "SuperAdmin:Email and SuperAdmin:Password must be configured."
            );
        }

        if (!await roleManager.RoleExistsAsync(Roles.SuperAdmin))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(Roles.SuperAdmin));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{Roles.SuperAdmin}': "
                    + string.Join(", ", roleResult.Errors.Select(error => error.Description))
                );
            }
        }

        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

        if (superAdmin is null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FullName = "MD Rashedul Hasan",
            };

            var userResult = await userManager.CreateAsync(superAdmin, superAdminPassword);

            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create SuperAdmin user: "
                    + string.Join(", ", userResult.Errors.Select(error => error.Description))
                );
            }
        }

        if (!await userManager.IsInRoleAsync(superAdmin, Roles.SuperAdmin))
        {
            var roleResult = await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role '{Roles.SuperAdmin}' to '{superAdminEmail}': "
                    + string.Join(", ", roleResult.Errors.Select(error => error.Description))
                );
            }
        }
        


    }
}