using Picklink.Application.DTOs;

namespace Picklink.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
