using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Picklink.Api.Services;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Application.Interfaces;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Domain.Enums;
using Picklink.Infrastructure.Data;

namespace Picklink.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/venues")]
public sealed class AdminVenuesController(PicklinkDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VenueListItemResponse>>>> GetVenues(
        [FromQuery] VenueQuery query,
        CancellationToken cancellationToken)
    {
        var venues = dbContext.Venues.AsNoTracking().Where(x => !x.IsDeleted)
            .Include(x => x.Province).Include(x => x.Ward).Include(x => x.Images).Include(x => x.Courts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            venues = venues.Where(x => x.Name.Contains(query.Search) || x.StreetAddress.Contains(query.Search));
        if (query.ProvinceId.HasValue) venues = venues.Where(x => x.ProvinceId == query.ProvinceId);
        if (query.WardId.HasValue) venues = venues.Where(x => x.WardId == query.WardId);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VenueStatus>(query.Status, true, out var status))
            venues = venues.Where(x => x.Status == status);

        var totalItems = await venues.CountAsync(cancellationToken);
        var entities = await venues.OrderBy(x => x.Status == VenueStatus.PendingApproval ? 0 : 1)
            .ThenByDescending(x => x.SubmittedAt ?? x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        var items = entities.Select(VenueApiMappings.ToListItem).ToArray();
        var meta = new { query.Page, query.PageSize, totalItems, totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize) };
        return Ok(ApiResponse<IReadOnlyList<VenueListItemResponse>>.Ok(items, meta: meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VenueDetailResponse>>> GetVenue(Guid id, CancellationToken cancellationToken)
    {
        var venue = await VenueQueryBase().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Venue not found.", 404);
        return Ok(ApiResponse<VenueDetailResponse>.Ok(VenueApiMappings.ToDetail(venue)));
    }

    [HttpPost("{id:guid}/approve")]
    public Task<ActionResult<ApiResponse>> Approve(Guid id, CancellationToken cancellationToken) =>
        ReviewAsync(id, VenueStatus.Published, null, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    public Task<ActionResult<ApiResponse>> Reject(Guid id, ReviewVenueRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new AppException("Rejection reason is required.", 400);
        return ReviewAsync(id, VenueStatus.Rejected, request.Reason.Trim(), cancellationToken);
    }

    [HttpPost("{id:guid}/suspend")]
    public Task<ActionResult<ApiResponse>> Suspend(Guid id, ReviewVenueRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new AppException("Suspension reason is required.", 400);
        return ReviewAsync(id, VenueStatus.Suspended, request.Reason.Trim(), cancellationToken);
    }

    private async Task<ActionResult<ApiResponse>> ReviewAsync(
        Guid id, VenueStatus targetStatus, string? reason, CancellationToken cancellationToken)
    {
        var venue = await dbContext.Venues.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Venue not found.", 404);

        if (targetStatus is VenueStatus.Published or VenueStatus.Rejected && venue.Status != VenueStatus.PendingApproval)
            throw new AppException("Only pending venues can be approved or rejected.", 409);
        if (targetStatus == VenueStatus.Suspended && venue.Status != VenueStatus.Published)
            throw new AppException("Only published venues can be suspended.", 409);

        venue.Status = targetStatus;
        venue.RejectionReason = reason;
        venue.ReviewedAt = DateTimeOffset.UtcNow;
        venue.ReviewedBy = currentUserService.UserId ?? throw new AppException("Unauthorized.", 401);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok($"Venue status changed to {targetStatus}."));
    }

    private IQueryable<Venue> VenueQueryBase() => dbContext.Venues.AsNoTracking()
        .Include(x => x.Province).Include(x => x.Ward).Include(x => x.Amenities).Include(x => x.Images)
        .Include(x => x.OpeningHours).Include(x => x.Courts).ThenInclude(x => x.Images);
}
