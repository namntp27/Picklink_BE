using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var courts = dbContext.Courts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == EntityStatus.Active)
            .Include(x => x.Venue)
            .Include(x => x.Sport)
            .Include(x => x.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            courts = courts.Where(x => x.Name.Contains(query.Search) || x.Venue!.Name.Contains(query.Search));
        }

        if (query.SportId.HasValue)
        {
            courts = courts.Where(x => x.SportId == query.SportId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            courts = courts.Where(x => x.Venue!.City == query.City);
        }

        if (!string.IsNullOrWhiteSpace(query.District))
        {
            courts = courts.Where(x => x.Venue!.District == query.District);
        }

        if (query.MinPrice.HasValue)
        {
            courts = courts.Where(x => x.PricePerHour >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            courts = courts.Where(x => x.PricePerHour <= query.MaxPrice.Value);
        }

        courts = query.SortBy?.ToLowerInvariant() switch
        {
            "price" => query.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? courts.OrderBy(x => x.PricePerHour)
                : courts.OrderByDescending(x => x.PricePerHour),
            "name" => query.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? courts.OrderBy(x => x.Name)
                : courts.OrderByDescending(x => x.Name),
            _ => courts.OrderByDescending(x => x.CreatedAt)
        };

        var totalItems = await courts.CountAsync(cancellationToken);
        var items = await courts
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new CourtListItemResponse(
                x.Id,
                x.Name,
                x.Venue!.Name,
                x.Sport!.Name,
                x.Venue.City,
                x.Venue.District,
                x.PricePerHour,
                x.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var meta = new { query.Page, query.PageSize, totalItems, totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize) };
        return Ok(ApiResponse<IReadOnlyList<CourtListItemResponse>>.Ok(items, meta: meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourtDetailResponse>>> GetCourt(Guid id, CancellationToken cancellationToken)
    {
        var court = await dbContext.Courts
            .AsNoTracking()
            .Include(x => x.Venue)
            .Include(x => x.Sport)
            .Include(x => x.Features)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (court is null)
        {
            throw new AppException("Court not found.", 404);
        }

        var response = new CourtDetailResponse(
            court.Id,
            court.Name,
            court.Description,
            court.Venue!.Name,
            court.Venue.Address,
            court.Venue.City,
            court.Venue.District,
            court.Sport!.Name,
            court.PricePerHour,
            court.Features.Select(x => x.Name).ToArray(),
            court.Images.OrderBy(x => x.SortOrder).Select(x => x.Url).ToArray());

        return Ok(ApiResponse<CourtDetailResponse>.Ok(response));
    }
}
