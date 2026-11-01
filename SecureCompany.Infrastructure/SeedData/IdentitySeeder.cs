using Microsoft.AspNetCore.Identity;
using SecureCompany.Domain.Entities;

namespace SecureCompany.Infrastructure.SeedData
{
    public class IdentitySeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentitySeeder(RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task SeedRoleAsync()
        {
            await SeedRolesAsync();
            await SeedAdminUserAsync();
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = { "HR", "Manager", "Admin" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Failed to create role {role}: " +
                            string.Join(", ", result.Errors.Select(e => e.Description))
                        );
                    }
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            string adminEmail = "a7med.2001.bahej@gmail.com",
                adminUserName = "ahmed.Dev",
                adminPassword = "aG@123456";

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                existingAdmin = new ApplicationUser
                {
                    Email = adminEmail,
                    EmailConfirmed = true,
                    UserName = adminUserName,
                    FullName = "System Adminstrative",
                    Department = "IT"
                };

                var result = await _userManager.CreateAsync(existingAdmin, adminPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create default admin user: " +
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
                }

                if (!await _userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    var roleResult = await _userManager.AddToRoleAsync(
                        existingAdmin,
                        "Admin"
                    );

                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join(", ", roleResult.Errors.Select(e => e.Description))
                        );
                    }
                }
            }
        }
    }
}