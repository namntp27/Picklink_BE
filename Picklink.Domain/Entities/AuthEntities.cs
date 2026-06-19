using Picklink.Domain.Common;

namespace Picklink.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokeReason { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }

    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? ipAddress, string? reason, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = ipAddress;
        RevokeReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

public sealed class ExternalLogin : BaseEntity
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? Email { get; set; }
}
