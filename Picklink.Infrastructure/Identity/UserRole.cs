using Microsoft.AspNetCore.Identity;

namespace Picklink.Infrastructure.Identity;

public sealed class UserRole : IdentityUserRole<Guid>
{
    public User? User { get; set; }
    public Role? Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
