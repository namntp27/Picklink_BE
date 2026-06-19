namespace Picklink.Application.DTOs;

public sealed record CreateBookingRequest(
    Guid CourtId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Note);

public sealed record BookingResponse(
    Guid Id,
    Guid CourtId,
    string CourtName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    decimal TotalAmount,
    string Status);
