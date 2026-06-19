using Microsoft.AspNetCore.Identity;
using Picklink.Domain.Enums;

namespace Picklink.Infrastructure.Identity;

public sealed class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
