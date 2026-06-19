using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Infrastructure.Identity;

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
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role(roleName, $"{roleName} role"));
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
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        const string email = "admin@picklink.local";
        var adminUser = await userManager.FindByEmailAsync(email);
        if (adminUser is null)
        {
            adminUser = new User
            {
                FullName = "Picklink Admin",
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        }

        if (!await dbContext.Admins.AnyAsync(x => x.UserId == adminUser.Id, cancellationToken))
        {
            dbContext.Admins.Add(new Admin { UserId = adminUser.Id, Department = "System" });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
