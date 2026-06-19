using Picklink.Application.Common;

namespace Picklink.Application.DTOs;

public sealed record ProvinceResponse(Guid Id, int Code, string Name);

public sealed record WardResponse(Guid Id, int Code, string Name);

public sealed record VenueImageInput(string Url, int SortOrder, bool IsPrimary);

public sealed record OpeningHourInput(
    DayOfWeek DayOfWeek,
    bool IsClosed,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime);

public sealed record CreateVenueRequest(
    string Name,
    string? Description,
    string StreetAddress,
    Guid ProvinceId,
    Guid WardId,
    string PhoneNumber,
    decimal Latitude,
    decimal Longitude,
    IReadOnlyList<string>? Amenities,
    IReadOnlyList<VenueImageInput>? Images,
    IReadOnlyList<OpeningHourInput>? OpeningHours);

public sealed record UpdateVenueRequest(
    string Name,
    string? Description,
    string StreetAddress,
    Guid ProvinceId,
    Guid WardId,
    string PhoneNumber,
    decimal Latitude,
    decimal Longitude,
    IReadOnlyList<string>? Amenities,
    IReadOnlyList<VenueImageInput>? Images,
    IReadOnlyList<OpeningHourInput>? OpeningHours);

public sealed record CreateCourtRequest(
    Guid VenueId,
    string Name,
    string Code,
    decimal PricePerHour,
    int SlotDurationMinutes,
    string Status,
    IReadOnlyList<VenueImageInput>? Images);

public sealed record UpdateCourtRequest(
    string Name,
    string Code,
    decimal PricePerHour,
    int SlotDurationMinutes,
    string Status,
    IReadOnlyList<VenueImageInput>? Images);

public sealed record CreateBlockedSlotRequest(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason);

public sealed record ReviewVenueRequest(string? Reason);

public sealed class VenueQuery : PagedQuery
{
    public Guid? ProvinceId { get; set; }
    public Guid? WardId { get; set; }
    public string? Status { get; set; }
}

public sealed class CourtQuery : PagedQuery
{
    public Guid? VenueId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? WardId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

public sealed record OpeningHourResponse(
    DayOfWeek DayOfWeek,
    bool IsClosed,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime);

public sealed record VenueImageResponse(Guid Id, string Url, int SortOrder, bool IsPrimary);

public sealed record CourtSummaryResponse(
    Guid Id,
    string Name,
    string Code,
    decimal PricePerHour,
    int SlotDurationMinutes,
    string Status,
    string? ImageUrl);

public sealed record VenueListItemResponse(
    Guid Id,
    string Name,
    string ProvinceName,
    string WardName,
    string StreetAddress,
    decimal Latitude,
    decimal Longitude,
    string Status,
    int CourtCount,
    string? ImageUrl);

public sealed record VenueDetailResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    string? Description,
    string StreetAddress,
    Guid ProvinceId,
    string ProvinceName,
    Guid WardId,
    string WardName,
    string PhoneNumber,
    decimal Latitude,
    decimal Longitude,
    string Status,
    string? RejectionReason,
    IReadOnlyList<string> Amenities,
    IReadOnlyList<VenueImageResponse> Images,
    IReadOnlyList<OpeningHourResponse> OpeningHours,
    IReadOnlyList<CourtSummaryResponse> Courts);

public sealed record CourtListItemResponse(
    Guid Id,
    Guid VenueId,
    string Name,
    string Code,
    string VenueName,
    string ProvinceName,
    string WardName,
    decimal PricePerHour,
    int SlotDurationMinutes,
    string Status,
    decimal Latitude,
    decimal Longitude,
    string? ImageUrl);

public sealed record BlockedSlotResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason);

public sealed record CourtDetailResponse(
    Guid Id,
    Guid VenueId,
    string Name,
    string Code,
    string VenueName,
    string StreetAddress,
    string ProvinceName,
    string WardName,
    decimal Latitude,
    decimal Longitude,
    decimal PricePerHour,
    int SlotDurationMinutes,
    string Status,
    IReadOnlyList<VenueImageResponse> Images,
    IReadOnlyList<OpeningHourResponse> OpeningHours,
    IReadOnlyList<BlockedSlotResponse> BlockedSlots);
