using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Infrastructure.Identity;
using Picklink.Infrastructure.Options;

namespace Picklink.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PicklinkDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await SeedRolesAsync(roleManager);
        await SeedSportsAsync(dbContext, cancellationToken);
        await SeedAdminAsync(userManager, dbContext, configuration, cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        var roleDescriptions = new Dictionary<string, string>
        {
            [AppRoles.Admin] = "System administrator with full platform access.",
            [AppRoles.Owner] = "Court owner who manages venues, courts, bookings and revenue.",
            [AppRoles.Player] = "Player who books courts, joins matches and uses community features."
        };

        foreach (var roleName in AppRoles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                var createResult = await roleManager.CreateAsync(new Role(roleName, roleDescriptions[roleName]));
                EnsureIdentitySuccess(createResult, $"Cannot seed role {roleName}.");
                continue;
            }

            if (role.Description != roleDescriptions[roleName])
            {
                role.Description = roleDescriptions[roleName];
                var updateResult = await roleManager.UpdateAsync(role);
                EnsureIdentitySuccess(updateResult, $"Cannot update role {roleName}.");
            }
        }
    }

    private static async Task SeedSportsAsync(PicklinkDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Sports.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Sports.AddRange(
            new Sport { Name = "Badminton", Slug = "badminton" },
            new Sport { Name = "Tennis", Slug = "tennis" },
            new Sport { Name = "Football", Slug = "football" },
            new Sport { Name = "Pickleball", Slug = "pickleball" });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminAsync(
        UserManager<User> userManager,
        PicklinkDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var seedOptions = ReadSeedOptions(configuration);
        var password = seedOptions.AdminPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var email = seedOptions.AdminEmail.Trim().ToLowerInvariant();
        var adminUser = await userManager.FindByEmailAsync(email);
        if (adminUser is null)
        {
            adminUser = new User
            {
                FullName = seedOptions.AdminFullName.Trim(),
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, password);
            EnsureIdentitySuccess(createResult, "Cannot seed admin user.");
        }
        else
        {
            var changed = false;
            if (adminUser.FullName != seedOptions.AdminFullName)
            {
                adminUser.FullName = seedOptions.AdminFullName;
                changed = true;
            }

            if (!adminUser.EmailConfirmed)
            {
                adminUser.EmailConfirmed = true;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(adminUser);
                EnsureIdentitySuccess(updateResult, "Cannot update admin user.");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            EnsureIdentitySuccess(addRoleResult, "Cannot assign Admin role.");
        }

        if (!await dbContext.Admins.AnyAsync(x => x.UserId == adminUser.Id, cancellationToken))
        {
            dbContext.Admins.Add(new Admin { UserId = adminUser.Id, Department = "System" });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static SeedOptions ReadSeedOptions(IConfiguration configuration)
    {
        return new SeedOptions
        {
            AdminEmail = configuration["Seed:AdminEmail"] ?? "admin@picklink.local",
            AdminFullName = configuration["Seed:AdminFullName"] ?? "Picklink Admin",
            AdminPassword = configuration["Seed:AdminPassword"]
        };
    }

    private static void EnsureIdentitySuccess(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }
}
