using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Picklink.Api.Services;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Domain.Enums;
using Picklink.Infrastructure.Data;

namespace Picklink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CourtsController(PicklinkDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CourtListItemResponse>>>> GetCourts(
        [FromQuery] CourtQuery query,
        CancellationToken cancellationToken)
    {
        var courts = CourtQueryBase()
            .Where(x => !x.IsDeleted && x.Venue!.Status == VenueStatus.Published && x.Status != CourtStatus.Inactive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            courts = courts.Where(x => x.Name.Contains(query.Search) || x.Code.Contains(query.Search) || x.Venue!.Name.Contains(query.Search));
        }
        if (query.VenueId.HasValue) courts = courts.Where(x => x.VenueId == query.VenueId);
        if (query.ProvinceId.HasValue) courts = courts.Where(x => x.Venue!.ProvinceId == query.ProvinceId);
        if (query.WardId.HasValue) courts = courts.Where(x => x.Venue!.WardId == query.WardId);
        if (query.MinPrice.HasValue) courts = courts.Where(x => x.PricePerHour >= query.MinPrice);
        if (query.MaxPrice.HasValue) courts = courts.Where(x => x.PricePerHour <= query.MaxPrice);

        courts = query.SortBy?.ToLowerInvariant() switch
        {
            "price" => query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? courts.OrderByDescending(x => x.PricePerHour)
                : courts.OrderBy(x => x.PricePerHour),
            "name" => courts.OrderBy(x => x.Name),
            _ => courts.OrderByDescending(x => x.CreatedAt)
        };

        var totalItems = await courts.CountAsync(cancellationToken);
        var entities = await courts.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        var items = entities.Select(VenueApiMappings.ToCourtListItem).ToArray();
        var meta = new { query.Page, query.PageSize, totalItems, totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize) };
        return Ok(ApiResponse<IReadOnlyList<CourtListItemResponse>>.Ok(items, meta: meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourtDetailResponse>>> GetCourt(Guid id, CancellationToken cancellationToken)
    {
        var court = await CourtDetailQuery()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.Venue!.Status == VenueStatus.Published, cancellationToken)
            ?? throw new AppException("Court not found.", 404);

        return Ok(ApiResponse<CourtDetailResponse>.Ok(VenueApiMappings.ToCourtDetail(court)));
    }

    private IQueryable<Picklink.Domain.Entities.Court> CourtQueryBase() => dbContext.Courts.AsNoTracking()
        .Include(x => x.Images)
        .Include(x => x.Venue).ThenInclude(x => x!.Province)
        .Include(x => x.Venue).ThenInclude(x => x!.Ward)
        .Include(x => x.Venue).ThenInclude(x => x!.Images);

    private IQueryable<Picklink.Domain.Entities.Court> CourtDetailQuery() => CourtQueryBase()
        .Include(x => x.Venue).ThenInclude(x => x!.OpeningHours)
        .Include(x => x.BlockedSlots);
}
