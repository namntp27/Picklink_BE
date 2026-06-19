using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Application.Interfaces;
using Picklink.Domain.Constants;
using Picklink.Domain.Entities;
using Picklink.Domain.Enums;
using Picklink.Infrastructure.Data;

namespace Picklink.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Player)]
[Route("api/[controller]")]
public sealed class BookingsController(PicklinkDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookingResponse>>> CreateBooking(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var playerId = RequireUserId();
        if (request.EndTime <= request.StartTime)
        {
            throw new AppException("End time must be after start time.", 400);
        }

        var court = await dbContext.Courts
            .Include(x => x.Venue)
            .FirstOrDefaultAsync(x => x.Id == request.CourtId && x.Status == EntityStatus.Active, cancellationToken);

        if (court is null)
        {
            throw new AppException("Court not found.", 404);
        }

        var hasOverlap = await dbContext.Bookings.AnyAsync(x =>
            x.CourtId == request.CourtId &&
            (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed) &&
            x.StartTime < request.EndTime &&
            request.StartTime < x.EndTime,
            cancellationToken);

        if (hasOverlap)
        {
            throw new AppException("This court already has a booking in the selected time range.", 409);
        }

        var hours = (decimal)(request.EndTime - request.StartTime).TotalHours;
        var booking = new Booking
        {
            PlayerId = playerId,
            CourtId = request.CourtId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalAmount = Math.Round(hours * court.PricePerHour, 2),
            Status = BookingStatus.Pending,
            Note = request.Note
        };
        booking.Participants.Add(new BookingParticipant { UserId = playerId, Status = ParticipantStatus.Accepted });

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BookingResponse>.Ok(ToResponse(booking, court.Name), "Booking created."));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BookingResponse>>>> GetMyBookings(CancellationToken cancellationToken)
    {
        var playerId = RequireUserId();
        var bookings = await dbContext.Bookings
            .AsNoTracking()
            .Include(x => x.Court)
            .Where(x => x.PlayerId == playerId)
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

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var playerId = RequireUserId();
        var booking = await dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.PlayerId == playerId, cancellationToken);
        if (booking is null)
        {
            throw new AppException("Booking not found.", 404);
        }

        booking.Status = BookingStatus.Cancelled;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Booking cancelled."));
    }

    private Guid RequireUserId()
    {
        if (currentUserService.UserId is not { } userId)
        {
            throw new AppException("Unauthorized.", 401);
        }

        return userId;
    }

    private static BookingResponse ToResponse(Booking booking, string courtName)
        => new(booking.Id, booking.CourtId, courtName, booking.StartTime, booking.EndTime, booking.TotalAmount, booking.Status.ToString());
}
