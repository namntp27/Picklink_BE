using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Picklink.Application.Interfaces;
using Picklink.Infrastructure.Data;
using Picklink.Infrastructure.Identity;
using Picklink.Infrastructure.Options;
using Picklink.Infrastructure.Services;
using Picklink.Infrastructure.Storage;

namespace Picklink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = configuration["Jwt:Issuer"] ?? options.Issuer;
            options.Audience = configuration["Jwt:Audience"] ?? options.Audience;
            options.SecretKey = configuration["Jwt:SecretKey"] ?? options.SecretKey;
            options.AccessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var accessTokenMinutes)
                ? accessTokenMinutes
                : options.AccessTokenMinutes;
            options.RefreshTokenDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var refreshTokenDays)
                ? refreshTokenDays
                : options.RefreshTokenDays;
        });
        services.Configure<StorageOptions>(options =>
        {
            options.UploadRoot = configuration["Storage:UploadRoot"] ?? options.UploadRoot;
            options.PublicBasePath = configuration["Storage:PublicBasePath"] ?? options.PublicBasePath;
            options.MaxFileBytes = long.TryParse(configuration["Storage:MaxFileBytes"], out var maxFileBytes)
                ? maxFileBytes
                : options.MaxFileBytes;
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<PicklinkDbContext>(options => options.UseSqlServer(connectionString));

        services.AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<PicklinkDbContext>();

        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
