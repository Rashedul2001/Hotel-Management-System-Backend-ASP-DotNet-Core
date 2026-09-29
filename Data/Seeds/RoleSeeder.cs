using Hotel_Management_System_Backend_dotNet.Entities.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Hotel_Management_System_Backend_dotNet.Data.Seeds;

public static class RoleSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles =
        [
            Roles.SuperAdmin,
            Roles.Admin,
            Roles.Staff,
            Roles.Guest
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role)
                );

                if (!result.Succeeded)
                {
                    throw new Exception(
                        $"Failed to create role '{role}'."
                    );
                }
            }
        }
    }
}