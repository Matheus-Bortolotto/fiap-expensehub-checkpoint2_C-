using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Identity;

internal static class IdentitySeeder
{
    internal static async Task SeedAsync(IServiceProvider services)
    {
        RoleManager<IdentityRole> roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        UserManager<ApplicationUser> userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        IConfiguration configuration =
            services.GetRequiredService<IConfiguration>();

        foreach (string roleName in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                IdentityResult roleResult =
                    await roleManager.CreateAsync(new IdentityRole(roleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create role '{roleName}'.");
                }
            }
        }

        string adminEmail =
            configuration["AdminSeed:Email"]
            ?? throw new InvalidOperationException(
                "AdminSeed:Email configuration was not found.");

        string adminPassword =
            configuration["AdminSeed:Password"]
            ?? throw new InvalidOperationException(
                "AdminSeed:Password configuration was not found.");

        ApplicationUser? admin =
            await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            IdentityResult userResult =
                await userManager.CreateAsync(admin, adminPassword);

            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the initial Admin user.");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, ApplicationRoles.Admin))
        {
            IdentityResult addRoleResult =
                await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);

            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign the Admin role to the initial user.");
            }
        }
    }
}
