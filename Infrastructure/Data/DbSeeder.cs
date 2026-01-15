using Microsoft.AspNetCore.Identity;
using BE.Core.Entities.Business;

namespace BE.Infrastructure.Data;

/// <summary>
/// Database Seeder - Tạo roles và admin user mặc định
/// </summary>
public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<User> userManager)
    {
        // Seed Roles
        string[] roleNames = { "Admin", "Staff", "Customer" };
        
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed Admin User
        var adminEmail = "admin@cinemax.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrator",
                EmailConfirmed = true,
                MembershipLevel = "Platinum",
                Points = 0,
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
