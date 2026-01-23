using Microsoft.AspNetCore.Identity;
using BE.Core.Entities.Business;
using BE.Data;

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

    /// <summary>
    /// Cập nhật tất cả showtimes cũ sang tương lai
    /// </summary>
    public static async Task UpdateShowtimesToFutureAsync(AppDbContext context)
    {
        var today = DateTime.Now.Date;
        var pastShowtimes = context.Showtimes
            .Where(st => st.StartTime < DateTime.Now)
            .ToList();

        if (pastShowtimes.Any())
        {
            foreach (var showtime in pastShowtimes)
            {
                // Tính số ngày đã qua
                var daysAgo = (today - showtime.StartTime.Date).Days;
                
                // Cập nhật sang ngày mai trở đi
                showtime.StartTime = showtime.StartTime.AddDays(daysAgo + 1);
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Đã cập nhật {pastShowtimes.Count} showtimes sang tương lai!");
        }
        else
        {
            Console.WriteLine("ℹ️ Không có showtimes nào cần cập nhật.");
        }
    }
}
