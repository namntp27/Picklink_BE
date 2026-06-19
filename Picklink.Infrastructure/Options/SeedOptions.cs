namespace Picklink.Infrastructure.Options;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; set; } = "admin@picklink.local";
    public string AdminFullName { get; set; } = "Picklink Admin";
    public string? AdminPassword { get; set; }
}
