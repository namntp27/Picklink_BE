namespace Picklink.Application.Interfaces;

public sealed record ExternalUserInfo(
    string Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerified,
    string? FullName,
    string? AvatarUrl);

public interface IExternalAuthVerifier
{
    Task<ExternalUserInfo> VerifyAsync(string provider, string token, CancellationToken cancellationToken = default);
}
