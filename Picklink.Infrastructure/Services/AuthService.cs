using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Application.Interfaces;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Infrastructure.Data;
using Picklink.Infrastructure.Identity;

namespace Picklink.Infrastructure.Services;

public sealed class AuthService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    PicklinkDbContext dbContext,
    JwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var role = NormalizeSelfRegisterRole(request.Role);
        if (!await roleManager.RoleExistsAsync(role))
        {
            throw new AppException("Role is not configured.", 400);
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new AppException("Email already exists.", 409);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new AppException("Cannot create user.", 400, result.Errors.ToDictionary(x => x.Code, x => x.Description));
        }

        await userManager.AddToRoleAsync(user, role);
        await CreateRoleRecordAsync(user.Id, role, cancellationToken);

        return await IssueTokensAsync(user, ipAddress, deviceInfo, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AppException("Invalid email or password.", 401);
        }

        if (user.IsDeleted)
        {
            throw new AppException("Account is no longer available.", 403);
        }

        return await IssueTokensAsync(user, ipAddress, deviceInfo, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = jwtTokenService.HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            throw new AppException("Refresh token is invalid or expired.", 401);
        }

        var user = await userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user is null)
        {
            throw new AppException("User not found.", 404);
        }

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        var response = await IssueTokensAsync(user, ipAddress, deviceInfo, cancellationToken);
        refreshToken.ReplacedByTokenHash = jwtTokenService.HashToken(response.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = jwtTokenService.HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return;
        }

        refreshToken.RevokedAt ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new AppException("User not found.", 404);
        }

        return await MapUserAsync(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = jwtTokenService.HashToken(refreshToken);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = jwtTokenService.GetRefreshTokenExpiry(),
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.Token,
            refreshToken,
            accessToken.ExpiresAt,
            await MapUserAsync(user));
    }

    private async Task<UserResponse> MapUserAsync(User user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserResponse(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.AvatarUrl,
            roles.ToArray());
    }

    private async Task CreateRoleRecordAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        if (role == AppRoles.Player && !await dbContext.Players.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            dbContext.Players.Add(new Player { UserId = userId });
        }

        if (role == AppRoles.Owner && !await dbContext.Owners.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            dbContext.Owners.Add(new Owner { UserId = userId, BusinessName = "New owner" });
        }

        if (role == AppRoles.Admin && !await dbContext.Admins.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            dbContext.Admins.Add(new Admin { UserId = userId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeSelfRegisterRole(string role)
    {
        if (string.Equals(role, AppRoles.Owner, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoles.Owner;
        }

        if (string.Equals(role, AppRoles.Player, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoles.Player;
        }

        throw new AppException("Only Player or Owner can self-register.", 400);
    }
}
