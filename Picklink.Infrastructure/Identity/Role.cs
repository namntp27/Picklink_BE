using Microsoft.AspNetCore.Identity;

namespace Picklink.Infrastructure.Identity;

public sealed class Role : IdentityRole<Guid>
{
    public Role()
    {
    }

    public Role(string name, string? description = null)
        : base(name)
    {
        Description = description;
    }

    public string? Description { get; set; }
}
