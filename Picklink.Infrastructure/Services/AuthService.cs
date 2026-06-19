using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Application.Interfaces;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Domain.Enums;
using Picklink.Infrastructure.Data;
using Picklink.Infrastructure.Identity;

namespace Picklink.Infrastructure.Services;

public sealed class AuthService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    PicklinkDbContext dbContext,
    JwtTokenService jwtTokenService,
    IExternalAuthVerifier externalAuthVerifier) : IAuthService
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

        var email = request.Email.Trim().ToLowerInvariant();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new AppException("Email already exists.", 409);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            UserName = email,
            PhoneNumber = request.PhoneNumber?.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new AppException("Cannot create user.", 400, result.Errors.ToDictionary(x => x.Code, x => x.Description));
        }

        await userManager.AddToRoleAsync(user, role);
        await CreateRoleRecordAsync(user.Id, role, cancellationToken);

        var response = await IssueTokensAsync(user, ipAddress, deviceInfo, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AppException("Invalid email or password.", 401);
        }

        EnsureUserCanSignIn(user);

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
        EnsureUserCanSignIn(user);

        var response = await IssueTokensAsync(user, ipAddress, deviceInfo, cancellationToken, refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = jwtTokenService.HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return;
        }

        refreshToken.Revoke(ipAddress, "Logout");
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

    public async Task<AuthResponse> ExternalLoginAsync(
        string provider,
        ExternalLoginRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var providerName = NormalizeExternalProvider(provider);
        var externalUser = await externalAuthVerifier.VerifyAsync(providerName, request.Token, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var linkedLogin = await dbContext.ExternalLogins
            .FirstOrDefaultAsync(
                x => x.Provider == providerName && x.ProviderUserId == externalUser.ProviderUserId,
                cancellationToken);

        User? user;
        if (linkedLogin is not null)
        {
            user = await userManager.FindByIdAsync(linkedLogin.UserId.ToString());
            if (user is null)
            {
                throw new AppException("Linked user not found.", 404);
            }

            EnsureUserCanSignIn(user);
            var linkedResponse = await IssueTokensAsync(user, ipAddress, deviceInfo, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return linkedResponse;
        }

        var email = externalUser.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new AppException("External account does not expose an email address.", 400);
        }

        user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                FullName = string.IsNullOrWhiteSpace(externalUser.FullName)
                    ? email.Split('@')[0]
                    : externalUser.FullName.Trim(),
                Email = email,
                UserName = email,
                AvatarUrl = externalUser.AvatarUrl,
                EmailConfirmed = externalUser.EmailVerified
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                throw new AppException("Cannot create external user.", 400, createResult.Errors.ToDictionary(x => x.Code, x => x.Description));
            }

            var role = NormalizeExternalAccountRole(request.Role);
            var addRoleResult = await userManager.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                throw new AppException("Cannot assign role to external user.", 400, addRoleResult.Errors.ToDictionary(x => x.Code, x => x.Description));
            }

            await CreateRoleRecordAsync(user.Id, role, cancellationToken);
        }
        else
        {
            EnsureUserCanSignIn(user);
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                var role = NormalizeExternalAccountRole(request.Role);
                var addRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                {
                    throw new AppException("Cannot assign role to external user.", 400, addRoleResult.Errors.ToDictionary(x => x.Code, x => x.Description));
                }

                await CreateRoleRecordAsync(user.Id, role, cancellationToken);
            }

            var changed = false;
            if (externalUser.EmailVerified && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(externalUser.AvatarUrl))
            {
                user.AvatarUrl = externalUser.AvatarUrl;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new AppException("Cannot update external user.", 400, updateResult.Errors.ToDictionary(x => x.Code, x => x.Description));
                }
            }
        }

        dbContext.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = providerName,
            ProviderUserId = externalUser.ProviderUserId,
            Email = email
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        var response = await IssueTokensAsync(user, ipAddress, deviceInfo, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken,
        RefreshToken? tokenToReplace = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = jwtTokenService.HashToken(refreshToken);

        tokenToReplace?.Revoke(ipAddress, "Rotated", refreshTokenHash);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = jwtTokenService.GetRefreshTokenExpiry(),
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo
        });

        if (tokenToReplace is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AuthResponse(
            accessToken.Token,
            refreshToken,
            "Bearer",
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
            user.Status.ToString(),
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

    private static string NormalizeExternalAccountRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return AppRoles.Player;
        }

        if (string.Equals(role, AppRoles.Owner, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoles.Owner;
        }

        if (string.Equals(role, AppRoles.Player, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoles.Player;
        }

        throw new AppException("External login can create only Player or Owner accounts.", 400);
    }

    private static string NormalizeExternalProvider(string provider)
    {
        if (string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            return "Google";
        }

        throw new AppException("Unsupported external login provider.", 400);
    }

    private static void EnsureUserCanSignIn(User user)
    {
        if (user.IsDeleted)
        {
            throw new AppException("Account is no longer available.", 403);
        }

        if (user.Status is UserStatus.Suspended or UserStatus.Locked)
        {
            throw new AppException("Account is not allowed to sign in.", 403);
        }
    }
}
