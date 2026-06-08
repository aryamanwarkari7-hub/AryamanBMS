using AryamanBMS.Models;
using Microsoft.AspNetCore.Identity;

namespace AryamanBMS.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUserModel>>();

            string[] roles =
            {
                "Admin",
                "HR",
                "Employee"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            var adminUser =
                await userManager.FindByNameAsync("admin");

            if (adminUser == null)
            {
                adminUser = new ApplicationUserModel
                {
                    UserName = "admin",
                    Email = "admin@aryamanbms.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    adminUser,
                    "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        adminUser,
                        "Admin");
                }
            }
        }
    }
}