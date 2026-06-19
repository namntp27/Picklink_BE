using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class Booking : BaseEntity
{
    public Guid PlayerId { get; set; }
    public Guid CourtId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public decimal TotalAmount { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? Note { get; set; }
    public string? CancelReason { get; set; }
    public Court? Court { get; set; }
    public ICollection<BookingParticipant> Participants { get; set; } = [];
}

public sealed class BookingParticipant : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Accepted;
    public Booking? Booking { get; set; }
}

public sealed class PaymentTransaction : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset? PaidAt { get; set; }
    public Booking? Booking { get; set; }
}

public sealed class Review : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid CourtId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public Booking? Booking { get; set; }
    public Court? Court { get; set; }
}
