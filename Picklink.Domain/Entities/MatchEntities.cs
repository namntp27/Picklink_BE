using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class MatchRequest : BaseEntity
{
    public Guid CreatorId { get; set; }
    public Guid SportId { get; set; }
    public Guid? VenueId { get; set; }
    public Guid? CourtId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? SkillLevel { get; set; }
    public int NeededPlayers { get; set; }
    public Visibility Visibility { get; set; } = Visibility.Public;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public Sport? Sport { get; set; }
    public Venue? Venue { get; set; }
    public Court? Court { get; set; }
    public ICollection<MatchParticipant> Participants { get; set; } = [];
    public ICollection<MatchInvite> Invites { get; set; } = [];
}

public sealed class MatchParticipant : BaseEntity
{
    public Guid MatchRequestId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    public MatchRequest? MatchRequest { get; set; }
}

public sealed class MatchInvite : BaseEntity
{
    public Guid MatchRequestId { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    public string? Message { get; set; }
    public MatchRequest? MatchRequest { get; set; }
}
