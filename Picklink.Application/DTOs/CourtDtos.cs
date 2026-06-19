using Picklink.Application.Common;

namespace Picklink.Application.DTOs;

public sealed class CourtQuery : PagedQuery
{
    public Guid? SportId { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

public sealed record CourtListItemResponse(
    Guid Id,
    string Name,
    string VenueName,
    string SportName,
    string City,
    string? District,
    decimal PricePerHour,
    string? ImageUrl);

public sealed record CourtDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string VenueName,
    string Address,
    string City,
    string? District,
    string SportName,
    decimal PricePerHour,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> Images);

public sealed record CreateVenueRequest(
    string Name,
    string? Description,
    string Address,
    string City,
    string? District,
    string? Ward);

public sealed record CreateCourtRequest(
    Guid VenueId,
    Guid SportId,
    string Name,
    string? Description,
    decimal PricePerHour,
    IReadOnlyList<string>? Features);
