using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Application.Interfaces;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Infrastructure.Data;

namespace Picklink.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Owner)]
[Route("api/owner")]
public sealed class OwnerController(PicklinkDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("venues")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateVenue(CreateVenueRequest request, CancellationToken cancellationToken)
    {
        var ownerId = RequireUserId();
        var venue = new Venue
        {
            OwnerId = ownerId,
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            City = request.City,
            District = request.District,
            Ward = request.Ward
        };

        dbContext.Venues.Add(venue);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(venue.Id, "Venue created."));
    }

    [HttpPost("courts")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCourt(CreateCourtRequest request, CancellationToken cancellationToken)
    {
        var ownerId = RequireUserId();
        var ownsVenue = await dbContext.Venues.AnyAsync(x => x.Id == request.VenueId && x.OwnerId == ownerId, cancellationToken);
        if (!ownsVenue)
        {
            throw new AppException("Venue not found or not owned by current user.", 404);
        }

        var sportExists = await dbContext.Sports.AnyAsync(x => x.Id == request.SportId, cancellationToken);
        if (!sportExists)
        {
            throw new AppException("Sport not found.", 404);
        }

        var court = new Court
        {
            VenueId = request.VenueId,
            SportId = request.SportId,
            Name = request.Name,
            Description = request.Description,
            PricePerHour = request.PricePerHour
        };

        foreach (var feature in request.Features ?? [])
        {
            court.Features.Add(new CourtFeature { Name = feature });
        }

        dbContext.Courts.Add(court);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(court.Id, "Court created."));
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BookingResponse>>>> GetOwnerBookings(CancellationToken cancellationToken)
    {
        var ownerId = RequireUserId();
        var bookings = await dbContext.Bookings
            .AsNoTracking()
            .Include(x => x.Court)
            .ThenInclude(x => x!.Venue)
            .Where(x => x.Court!.Venue!.OwnerId == ownerId)
            .OrderByDescending(x => x.StartTime)
            .Select(x => new BookingResponse(
                x.Id,
                x.CourtId,
                x.Court!.Name,
                x.StartTime,
                x.EndTime,
                x.TotalAmount,
                x.Status.ToString()))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<BookingResponse>>.Ok(bookings));
    }

    private Guid RequireUserId()
    {
        if (currentUserService.UserId is not { } userId)
        {
            throw new AppException("Unauthorized.", 401);
        }

        return userId;
    }
}
