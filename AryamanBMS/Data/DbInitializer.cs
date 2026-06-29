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

            var configuration =
                serviceProvider.GetRequiredService<IConfiguration>();

            string[] roles =
            {
                "Admin",
                "HR",
                "Employee",
                "ProjectManager"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            var adminUserName =
                configuration["SeedAdmin:UserName"];

            var adminEmail =
                configuration["SeedAdmin:Email"];

            var adminPassword =
                configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminUserName) ||
                string.IsNullOrWhiteSpace(adminEmail) ||
                string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var adminUser =
                await userManager.FindByNameAsync(adminUserName);

            if (adminUser == null)
            {
                adminUser = new ApplicationUserModel
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    adminUser,
                    adminPassword);

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