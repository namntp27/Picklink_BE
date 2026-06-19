using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Domain.Enums;
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
        await SeedAdministrativeUnitsAsync(dbContext, cancellationToken);
        await SeedSportsAsync(dbContext, cancellationToken);
        await SeedAdminAsync(userManager, dbContext, configuration, cancellationToken);
        await SeedDemoOwnerAsync(userManager, dbContext, configuration, cancellationToken);
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
                EnsureIdentitySuccess(
                    await roleManager.CreateAsync(new Role(roleName, roleDescriptions[roleName])),
                    $"Cannot seed role {roleName}.");
                continue;
            }

            if (role.Description != roleDescriptions[roleName])
            {
                role.Description = roleDescriptions[roleName];
                EnsureIdentitySuccess(await roleManager.UpdateAsync(role), $"Cannot update role {roleName}.");
            }
        }
    }

    private static async Task SeedAdministrativeUnitsAsync(PicklinkDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.AdministrativeProvinces.AnyAsync(cancellationToken))
        {
            return;
        }

        const string resourceName = "Picklink.Infrastructure.Data.Seed.vietnam_admin_units.json";
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource {resourceName} was not found.");

        var source = await JsonSerializer.DeserializeAsync<List<ProvinceSeed>>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Administrative unit seed data is invalid.");

        var provinces = source.Select(item => new AdministrativeProvince
        {
            Code = item.Code,
            Name = item.Name,
            CodeName = item.CodeName,
            DivisionType = item.DivisionType,
            Wards = item.Wards.Select(ward => new AdministrativeWard
            {
                Code = ward.Code,
                Name = ward.Name,
                CodeName = ward.CodeName,
                DivisionType = ward.DivisionType
            }).ToList()
        });

        dbContext.AdministrativeProvinces.AddRange(provinces);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSportsAsync(PicklinkDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Sports.AnyAsync(x => x.Slug == "pickleball", cancellationToken))
        {
            return;
        }

        dbContext.Sports.Add(new Sport { Name = "Pickleball", Slug = "pickleball" });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminAsync(
        UserManager<User> userManager,
        PicklinkDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var options = ReadSeedOptions(configuration);
        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            return;
        }

        var adminUser = await EnsureUserAsync(
            userManager,
            options.AdminEmail,
            options.AdminFullName,
            options.AdminPassword,
            AppRoles.Admin);

        if (!await dbContext.Admins.AnyAsync(x => x.UserId == adminUser.Id, cancellationToken))
        {
            dbContext.Admins.Add(new Admin { UserId = adminUser.Id, Department = "System" });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedDemoOwnerAsync(
        UserManager<User> userManager,
        PicklinkDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var options = ReadSeedOptions(configuration);
        if (string.IsNullOrWhiteSpace(options.OwnerPassword))
        {
            return;
        }

        var ownerUser = await EnsureUserAsync(
            userManager,
            options.OwnerEmail,
            options.OwnerFullName,
            options.OwnerPassword,
            AppRoles.Owner);

        if (!await dbContext.Owners.AnyAsync(x => x.UserId == ownerUser.Id, cancellationToken))
        {
            dbContext.Owners.Add(new Owner { UserId = ownerUser.Id });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await dbContext.Venues.AnyAsync(x => x.OwnerId == ownerUser.Id, cancellationToken))
        {
            return;
        }

        var province = await dbContext.AdministrativeProvinces
            .Include(x => x.Wards)
            .FirstAsync(x => x.Code == 1, cancellationToken);
        var ward = province.Wards.OrderBy(x => x.Code).First();
        var sport = await dbContext.Sports.FirstAsync(x => x.Slug == "pickleball", cancellationToken);
        const string imageUrl = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?auto=format&fit=crop&w=1200&q=80";

        var venue = new Venue
        {
            OwnerId = ownerUser.Id,
            ProvinceId = province.Id,
            WardId = ward.Id,
            Name = "Picklink Pickleball Center",
            Description = "Cụm sân Pickleball mẫu phục vụ trình diễn hệ thống.",
            StreetAddress = "12 Đường Picklink",
            PhoneNumber = "0901234567",
            Latitude = 21.028511m,
            Longitude = 105.804817m,
            Status = VenueStatus.Published,
            SubmittedAt = DateTimeOffset.UtcNow,
            ReviewedAt = DateTimeOffset.UtcNow,
            Amenities =
            [
                new VenueAmenity { Name = "Bãi đỗ xe" },
                new VenueAmenity { Name = "Phòng thay đồ" },
                new VenueAmenity { Name = "Cho thuê vợt" }
            ],
            Images = [new VenueImage { Url = imageUrl, SortOrder = 0, IsPrimary = true }],
            OpeningHours = Enumerable.Range(0, 7)
                .Select(day => new VenueOpeningHour
                {
                    DayOfWeek = (DayOfWeek)day,
                    OpenTime = new TimeOnly(6, 0),
                    CloseTime = new TimeOnly(22, 0)
                })
                .ToList(),
            Courts =
            [
                CreateDemoCourt(sport.Id, "Sân Picklink 01", "PK-01", 200000m, imageUrl),
                CreateDemoCourt(sport.Id, "Sân Picklink 02", "PK-02", 180000m, imageUrl)
            ]
        };

        dbContext.Venues.Add(venue);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Court CreateDemoCourt(Guid sportId, string name, string code, decimal price, string imageUrl) => new()
    {
        SportId = sportId,
        Name = name,
        Code = code,
        PricePerHour = price,
        SlotDurationMinutes = 60,
        Status = CourtStatus.Available,
        Images = [new CourtImage { Url = imageUrl, SortOrder = 0, IsPrimary = true }]
    };

    private static async Task<User> EnsureUserAsync(
        UserManager<User> userManager,
        string emailValue,
        string fullName,
        string password,
        string role)
    {
        var email = emailValue.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                FullName = fullName.Trim(),
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };
            EnsureIdentitySuccess(await userManager.CreateAsync(user, password), $"Cannot seed {role} user.");
        }
        else if (!await userManager.CheckPasswordAsync(user, password))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            EnsureIdentitySuccess(
                await userManager.ResetPasswordAsync(user, resetToken, password),
                $"Cannot reset {role} demo password.");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            EnsureIdentitySuccess(await userManager.AddToRoleAsync(user, role), $"Cannot assign {role} role.");
        }

        return user;
    }

    private static SeedOptions ReadSeedOptions(IConfiguration configuration) => new()
    {
        AdminEmail = configuration["Seed:AdminEmail"] ?? "admin@picklink.local",
        AdminFullName = configuration["Seed:AdminFullName"] ?? "Picklink Admin",
        AdminPassword = configuration["Seed:AdminPassword"],
        OwnerEmail = configuration["Seed:OwnerEmail"] ?? "owner@picklink.local",
        OwnerFullName = configuration["Seed:OwnerFullName"] ?? "Picklink Demo Owner",
        OwnerPassword = configuration["Seed:OwnerPassword"]
    };

    private static void EnsureIdentitySuccess(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(x => x.Description))}");
        }
    }

    private sealed class ProvinceSeed
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("codename")]
        public string CodeName { get; set; } = string.Empty;

        [JsonPropertyName("division_type")]
        public string DivisionType { get; set; } = string.Empty;

        [JsonPropertyName("wards")]
        public List<WardSeed> Wards { get; set; } = [];
    }

    private sealed class WardSeed
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("codename")]
        public string CodeName { get; set; } = string.Empty;

        [JsonPropertyName("division_type")]
        public string DivisionType { get; set; } = string.Empty;
    }
}
