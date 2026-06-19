using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class Sport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public sealed class AdministrativeProvince : BaseEntity
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CodeName { get; set; } = string.Empty;
    public string DivisionType { get; set; } = string.Empty;
    public ICollection<AdministrativeWard> Wards { get; set; } = [];
}

public sealed class AdministrativeWard : BaseEntity
{
    public Guid ProvinceId { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CodeName { get; set; } = string.Empty;
    public string DivisionType { get; set; } = string.Empty;
    public AdministrativeProvince? Province { get; set; }
}

public sealed class Venue : BaseEntity
{
    public Guid OwnerId { get; set; }
    public Guid ProvinceId { get; set; }
    public Guid WardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StreetAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public VenueStatus Status { get; set; } = VenueStatus.Draft;
    public string? RejectionReason { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public AdministrativeProvince? Province { get; set; }
    public AdministrativeWard? Ward { get; set; }
    public ICollection<VenueAmenity> Amenities { get; set; } = [];
    public ICollection<VenueImage> Images { get; set; } = [];
    public ICollection<VenueOpeningHour> OpeningHours { get; set; } = [];
    public ICollection<Court> Courts { get; set; } = [];
}

public sealed class VenueAmenity : BaseEntity
{
    public Guid VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Venue? Venue { get; set; }
}

public sealed class VenueImage : BaseEntity
{
    public Guid VenueId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public Venue? Venue { get; set; }
}

public sealed class VenueOpeningHour : BaseEntity
{
    public Guid VenueId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public Venue? Venue { get; set; }
}

public sealed class Court : BaseEntity
{
    public Guid VenueId { get; set; }
    public Guid SportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal PricePerHour { get; set; }
    public int SlotDurationMinutes { get; set; } = 60;
    public CourtStatus Status { get; set; } = CourtStatus.Available;
    public Venue? Venue { get; set; }
    public Sport? Sport { get; set; }
    public ICollection<CourtImage> Images { get; set; } = [];
    public ICollection<CourtBlockedSlot> BlockedSlots { get; set; } = [];
}

public sealed class CourtImage : BaseEntity
{
    public Guid CourtId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
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
