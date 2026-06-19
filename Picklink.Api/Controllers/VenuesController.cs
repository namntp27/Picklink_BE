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
public sealed class VenuesController(PicklinkDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VenueListItemResponse>>>> GetVenues(
        [FromQuery] VenueQuery query,
        CancellationToken cancellationToken)
    {
        var venues = dbContext.Venues.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == VenueStatus.Published)
            .Include(x => x.Province)
            .Include(x => x.Ward)
            .Include(x => x.Images)
            .Include(x => x.Courts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            venues = venues.Where(x => x.Name.Contains(query.Search) || x.StreetAddress.Contains(query.Search));
        }

        if (query.ProvinceId.HasValue)
        {
            venues = venues.Where(x => x.ProvinceId == query.ProvinceId);
        }

        if (query.WardId.HasValue)
        {
            venues = venues.Where(x => x.WardId == query.WardId);
        }

        var totalItems = await venues.CountAsync(cancellationToken);
        var entities = await venues.OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var items = entities.Select(VenueApiMappings.ToListItem).ToArray();
        var meta = new { query.Page, query.PageSize, totalItems, totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize) };

        return Ok(ApiResponse<IReadOnlyList<VenueListItemResponse>>.Ok(items, meta: meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VenueDetailResponse>>> GetVenue(Guid id, CancellationToken cancellationToken)
    {
        var venue = await VenueQueryBase()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.Status == VenueStatus.Published, cancellationToken)
            ?? throw new AppException("Venue not found.", 404);

        return Ok(ApiResponse<VenueDetailResponse>.Ok(VenueApiMappings.ToDetail(venue)));
    }

    private IQueryable<Picklink.Domain.Entities.Venue> VenueQueryBase() => dbContext.Venues.AsNoTracking()
        .Include(x => x.Province)
        .Include(x => x.Ward)
        .Include(x => x.Amenities)
        .Include(x => x.Images)
        .Include(x => x.OpeningHours)
        .Include(x => x.Courts).ThenInclude(x => x.Images);
}
