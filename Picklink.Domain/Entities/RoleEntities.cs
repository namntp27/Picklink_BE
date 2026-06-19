using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class Player : BaseEntity
{
    public Guid UserId { get; set; }
    public string? SkillLevel { get; set; }
    public string? PreferredSports { get; set; }
    public string? Bio { get; set; }
    public int TotalBookings { get; set; }
    public int TotalMatches { get; set; }
}

public sealed class Owner : BaseEntity
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
}

public sealed class Admin : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Department { get; set; }
    public string? PermissionNote { get; set; }
}
