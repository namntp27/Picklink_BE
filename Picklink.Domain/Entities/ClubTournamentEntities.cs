using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class Club : BaseEntity
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public Visibility Visibility { get; set; } = Visibility.Public;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public ICollection<ClubMember> Members { get; set; } = [];
}

public sealed class ClubMember : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Member";
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    public Club? Club { get; set; }
}

public sealed class ClubPost : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid PostId { get; set; }
    public Club? Club { get; set; }
    public Post? Post { get; set; }
}

public sealed class Tournament : BaseEntity
{
    public Guid OrganizerId { get; set; }
    public Guid SportId { get; set; }
    public Guid? VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int MaxParticipants { get; set; }
    public decimal Fee { get; set; }
    public string? Prize { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public Sport? Sport { get; set; }
    public Venue? Venue { get; set; }
    public ICollection<TournamentParticipant> Participants { get; set; } = [];
}

public sealed class TournamentParticipant : BaseEntity
{
    public Guid TournamentId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    public Tournament? Tournament { get; set; }
}
