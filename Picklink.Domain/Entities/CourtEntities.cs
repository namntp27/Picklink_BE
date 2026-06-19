using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class Sport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public sealed class Venue : BaseEntity
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? Ward { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public ICollection<Court> Courts { get; set; } = [];
}

public sealed class Court : BaseEntity
{
    public Guid VenueId { get; set; }
    public Guid SportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerHour { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public Venue? Venue { get; set; }
    public Sport? Sport { get; set; }
    public ICollection<CourtImage> Images { get; set; } = [];
    public ICollection<CourtFeature> Features { get; set; } = [];
    public ICollection<CourtSchedule> Schedules { get; set; } = [];
    public ICollection<CourtBlockedSlot> BlockedSlots { get; set; } = [];
}

public sealed class CourtImage : BaseEntity
{
    public Guid CourtId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Court? Court { get; set; }
}

public sealed class CourtFeature : BaseEntity
{
    public Guid CourtId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Court? Court { get; set; }
}

public sealed class CourtSchedule : BaseEntity
{
    public Guid CourtId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public Court? Court { get; set; }
}

public sealed class CourtBlockedSlot : BaseEntity
{
    public Guid CourtId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? Reason { get; set; }
    public Court? Court { get; set; }
}
