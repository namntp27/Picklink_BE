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
[Authorize(Roles = AppRoles.Owner)]
[Route("api/owner")]
public sealed class OwnerController(PicklinkDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("venues")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VenueListItemResponse>>>> GetVenues(CancellationToken cancellationToken)
    {
        var ownerId = RequireUserId();
        var venues = await dbContext.Venues.AsNoTracking()
            .Where(x => x.OwnerId == ownerId && !x.IsDeleted)
            .Include(x => x.Province)
            .Include(x => x.Ward)
            .Include(x => x.Images)
            .Include(x => x.Courts)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<VenueListItemResponse>>.Ok(venues.Select(VenueApiMappings.ToListItem).ToArray()));
    }

    [HttpGet("venues/{id:guid}")]
    public async Task<ActionResult<ApiResponse<VenueDetailResponse>>> GetVenue(Guid id, CancellationToken cancellationToken)
    {
        var venue = await FindOwnedVenueAsync(id, RequireUserId(), cancellationToken);
        return Ok(ApiResponse<VenueDetailResponse>.Ok(VenueApiMappings.ToDetail(venue)));
    }

    [HttpPost("venues")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateVenue(CreateVenueRequest request, CancellationToken cancellationToken)
    {
        var ownerId = RequireUserId();
        await ValidateVenueInputAsync(request.Name, request.StreetAddress, request.PhoneNumber, request.ProvinceId, request.WardId,
            request.Latitude, request.Longitude, request.Amenities, request.Images, request.OpeningHours, cancellationToken);

        var venue = new Venue { OwnerId = ownerId, Status = VenueStatus.Draft };
        ApplyVenueValues(venue, request.Name, request.Description, request.StreetAddress, request.ProvinceId, request.WardId,
            request.PhoneNumber, request.Latitude, request.Longitude);
        ReplaceVenueChildren(venue, request.Amenities, request.Images, request.OpeningHours);

        dbContext.Venues.Add(venue);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Ok(venue.Id, "Venue created as draft."));
    }

    [HttpPut("venues/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateVenue(Guid id, UpdateVenueRequest request, CancellationToken cancellationToken)
    {
        var venue = await FindOwnedVenueAsync(id, RequireUserId(), cancellationToken, tracking: true);
        await ValidateVenueInputAsync(request.Name, request.StreetAddress, request.PhoneNumber, request.ProvinceId, request.WardId,
            request.Latitude, request.Longitude, request.Amenities, request.Images, request.OpeningHours, cancellationToken);

        ApplyVenueValues(venue, request.Name, request.Description, request.StreetAddress, request.ProvinceId, request.WardId,
            request.PhoneNumber, request.Latitude, request.Longitude);
        dbContext.VenueAmenities.RemoveRange(venue.Amenities);
        dbContext.VenueImages.RemoveRange(venue.Images);
        dbContext.VenueOpeningHours.RemoveRange(venue.OpeningHours);
        ReplaceVenueChildren(venue, request.Amenities, request.Images, request.OpeningHours);
        ResetVenueApproval(venue);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Venue updated and returned to draft."));
    }

    [HttpDelete("venues/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteVenue(Guid id, CancellationToken cancellationToken)
    {
        var venue = await FindOwnedVenueAsync(id, RequireUserId(), cancellationToken, tracking: true);
        var hasBookings = await dbContext.Bookings.AnyAsync(x => x.Court!.VenueId == venue.Id, cancellationToken);
        if (hasBookings) throw new AppException("Venue with bookings cannot be deleted.", 409);

        venue.IsDeleted = true;
        venue.DeletedAt = DateTimeOffset.UtcNow;
        foreach (var court in venue.Courts)
        {
            court.IsDeleted = true;
            court.DeletedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Venue deleted."));
    }

    [HttpPost("venues/{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse>> SubmitVenue(Guid id, CancellationToken cancellationToken)
    {
        var venue = await FindOwnedVenueAsync(id, RequireUserId(), cancellationToken, tracking: true);
        if (venue.Status is not (VenueStatus.Draft or VenueStatus.Rejected))
        {
            throw new AppException("Only draft or rejected venues can be submitted.", 409);
        }
        if (!venue.Courts.Any(x => !x.IsDeleted)) throw new AppException("Add at least one court before submitting.", 400);
        if (venue.Images.Count == 0) throw new AppException("Add at least one venue image before submitting.", 400);
        if (venue.OpeningHours.Count != 7) throw new AppException("Configure all seven opening days before submitting.", 400);

        venue.Status = VenueStatus.PendingApproval;
        venue.SubmittedAt = DateTimeOffset.UtcNow;
        venue.RejectionReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Venue submitted for approval."));
    }

    [HttpGet("courts")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CourtListItemResponse>>>> GetCourts(CancellationToken cancellationToken)
    {
        var ownerId = RequireUserId();
        var courts = await CourtQueryBase().Where(x => x.Venue!.OwnerId == ownerId && !x.IsDeleted)
            .OrderBy(x => x.Venue!.Name).ThenBy(x => x.Code).ToListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CourtListItemResponse>>.Ok(courts.Select(VenueApiMappings.ToCourtListItem).ToArray()));
    }

    [HttpGet("courts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourtDetailResponse>>> GetCourt(Guid id, CancellationToken cancellationToken)
    {
        var court = await FindOwnedCourtAsync(id, RequireUserId(), cancellationToken);
        return Ok(ApiResponse<CourtDetailResponse>.Ok(VenueApiMappings.ToCourtDetail(court)));
    }

    [HttpPost("courts")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCourt(CreateCourtRequest request, CancellationToken cancellationToken)
    {
        var ownerId = RequireUserId();
        var venue = await FindOwnedVenueAsync(request.VenueId, ownerId, cancellationToken, tracking: true);
        var status = ParseCourtStatus(request.Status);
        await ValidateCourtInputAsync(request.VenueId, null, request.Name, request.Code, request.PricePerHour,
            request.SlotDurationMinutes, request.Images, cancellationToken);
        var sportId = await dbContext.Sports.Where(x => x.Slug == "pickleball").Select(x => x.Id).FirstAsync(cancellationToken);

        var court = new Court
        {
            VenueId = venue.Id,
            SportId = sportId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            PricePerHour = request.PricePerHour,
            SlotDurationMinutes = request.SlotDurationMinutes,
            Status = status
        };
        AddCourtImages(court, request.Images);
        dbContext.Courts.Add(court);
        ResetVenueApproval(venue);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Ok(court.Id, "Court created."));
    }

    [HttpPut("courts/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateCourt(Guid id, UpdateCourtRequest request, CancellationToken cancellationToken)
    {
        var court = await FindOwnedCourtAsync(id, RequireUserId(), cancellationToken, tracking: true);
        await ValidateCourtInputAsync(court.VenueId, court.Id, request.Name, request.Code, request.PricePerHour,
            request.SlotDurationMinutes, request.Images, cancellationToken);

        court.Name = request.Name.Trim();
        court.Code = request.Code.Trim().ToUpperInvariant();
        court.PricePerHour = request.PricePerHour;
        court.SlotDurationMinutes = request.SlotDurationMinutes;
        court.Status = ParseCourtStatus(request.Status);
        dbContext.CourtImages.RemoveRange(court.Images);
        AddCourtImages(court, request.Images);
        ResetVenueApproval(court.Venue!);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Court updated."));
    }

    [HttpDelete("courts/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteCourt(Guid id, CancellationToken cancellationToken)
    {
        var court = await FindOwnedCourtAsync(id, RequireUserId(), cancellationToken, tracking: true);
        if (await dbContext.Bookings.AnyAsync(x => x.CourtId == id, cancellationToken))
        {
            throw new AppException("Court with bookings cannot be deleted.", 409);
        }

        court.IsDeleted = true;
        court.DeletedAt = DateTimeOffset.UtcNow;
        ResetVenueApproval(court.Venue!);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Court deleted."));
    }

    [HttpPost("courts/{id:guid}/blocked-slots")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddBlockedSlot(Guid id, CreateBlockedSlotRequest request, CancellationToken cancellationToken)
    {
        var court = await FindOwnedCourtAsync(id, RequireUserId(), cancellationToken, tracking: true);
        if (request.EndTime <= request.StartTime) throw new AppException("Maintenance end time must be after start time.", 400);
        var overlaps = await dbContext.CourtBlockedSlots.AnyAsync(x => x.CourtId == id && !x.IsDeleted &&
            x.StartTime < request.EndTime && request.StartTime < x.EndTime, cancellationToken);
        if (overlaps) throw new AppException("Maintenance period overlaps an existing period.", 409);

        var slot = new CourtBlockedSlot
        {
            CourtId = court.Id,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Reason = request.Reason?.Trim()
        };
        dbContext.CourtBlockedSlots.Add(slot);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Ok(slot.Id, "Maintenance period created."));
    }

    [HttpDelete("courts/{courtId:guid}/blocked-slots/{slotId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteBlockedSlot(Guid courtId, Guid slotId, CancellationToken cancellationToken)
    {
        await FindOwnedCourtAsync(courtId, RequireUserId(), cancellationToken);
        var slot = await dbContext.CourtBlockedSlots.FirstOrDefaultAsync(x => x.Id == slotId && x.CourtId == courtId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Maintenance period not found.", 404);
        slot.IsDeleted = true;
        slot.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Maintenance period deleted."));
    }

    private async Task<Venue> FindOwnedVenueAsync(Guid id, Guid ownerId, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = tracking ? dbContext.Venues.AsQueryable() : dbContext.Venues.AsNoTracking();
        return await query
            .Include(x => x.Province).Include(x => x.Ward).Include(x => x.Amenities).Include(x => x.Images)
            .Include(x => x.OpeningHours).Include(x => x.Courts).ThenInclude(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Venue not found.", 404);
    }

    private async Task<Court> FindOwnedCourtAsync(Guid id, Guid ownerId, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = tracking ? dbContext.Courts.AsQueryable() : dbContext.Courts.AsNoTracking();
        return await query.Include(x => x.Images).Include(x => x.BlockedSlots)
            .Include(x => x.Venue).ThenInclude(x => x!.Province)
            .Include(x => x.Venue).ThenInclude(x => x!.Ward)
            .Include(x => x.Venue).ThenInclude(x => x!.Images)
            .Include(x => x.Venue).ThenInclude(x => x!.OpeningHours)
            .FirstOrDefaultAsync(x => x.Id == id && x.Venue!.OwnerId == ownerId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Court not found.", 404);
    }

    private IQueryable<Court> CourtQueryBase() => dbContext.Courts.AsNoTracking().Include(x => x.Images)
        .Include(x => x.Venue).ThenInclude(x => x!.Province)
        .Include(x => x.Venue).ThenInclude(x => x!.Ward)
        .Include(x => x.Venue).ThenInclude(x => x!.Images);

    private async Task ValidateVenueInputAsync(
        string name, string address, string phone, Guid provinceId, Guid wardId, decimal latitude, decimal longitude,
        IReadOnlyList<string>? amenities, IReadOnlyList<VenueImageInput>? images, IReadOnlyList<OpeningHourInput>? openingHours,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) throw new AppException("Venue name is required and must not exceed 200 characters.", 400);
        if (string.IsNullOrWhiteSpace(address) || address.Trim().Length > 300) throw new AppException("Street address is required.", 400);
        if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length > 30) throw new AppException("Venue phone number is required.", 400);
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) throw new AppException("Venue coordinates are invalid.", 400);
        if (!await dbContext.AdministrativeWards.AnyAsync(x => x.Id == wardId && x.ProvinceId == provinceId, cancellationToken))
            throw new AppException("Province and ward do not match.", 400);
        if ((amenities?.Count ?? 0) > 20 || amenities?.Any(x => string.IsNullOrWhiteSpace(x) || x.Trim().Length > 120) == true)
            throw new AppException("Venue supports at most 20 valid amenities.", 400);
        ValidateImages(images, 10);
        ValidateOpeningHours(openingHours);
    }

    private async Task ValidateCourtInputAsync(Guid venueId, Guid? courtId, string name, string code, decimal price,
        int slotDuration, IReadOnlyList<VenueImageInput>? images, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160) throw new AppException("Court name is required.", 400);
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 50) throw new AppException("Court code is required.", 400);
        if (price <= 0) throw new AppException("Court price must be greater than zero.", 400);
        if (slotDuration is < 30 or > 180 || slotDuration % 30 != 0) throw new AppException("Slot duration must be 30-180 minutes in 30-minute steps.", 400);
        var normalizedCode = code.Trim().ToUpperInvariant();
        if (await dbContext.Courts.AnyAsync(x => x.VenueId == venueId && x.Code == normalizedCode && !x.IsDeleted && x.Id != courtId, cancellationToken))
            throw new AppException("Court code already exists in this venue.", 409);
        ValidateImages(images, 5);
    }

    private static void ValidateImages(IReadOnlyList<VenueImageInput>? images, int maximum)
    {
        if ((images?.Count ?? 0) > maximum) throw new AppException($"A maximum of {maximum} images is allowed.", 400);
        if (images?.Any(x => string.IsNullOrWhiteSpace(x.Url) || x.Url.Length > 500) == true) throw new AppException("Image URL is invalid.", 400);
    }

    private static void ValidateOpeningHours(IReadOnlyList<OpeningHourInput>? items)
    {
        if (items is null || items.Count != 7 || items.Select(x => x.DayOfWeek).Distinct().Count() != 7)
            throw new AppException("Opening hours must contain each day of the week exactly once.", 400);
        foreach (var item in items.Where(x => !x.IsClosed))
        {
            if (!item.OpenTime.HasValue || !item.CloseTime.HasValue || item.OpenTime >= item.CloseTime)
                throw new AppException("Opening hours are invalid.", 400);
        }
    }

    private static void ApplyVenueValues(Venue venue, string name, string? description, string address, Guid provinceId,
        Guid wardId, string phone, decimal latitude, decimal longitude)
    {
        venue.Name = name.Trim();
        venue.Description = description?.Trim();
        venue.StreetAddress = address.Trim();
        venue.ProvinceId = provinceId;
        venue.WardId = wardId;
        venue.PhoneNumber = phone.Trim();
        venue.Latitude = latitude;
        venue.Longitude = longitude;
    }

    private static void ReplaceVenueChildren(Venue venue, IReadOnlyList<string>? amenities,
        IReadOnlyList<VenueImageInput>? images, IReadOnlyList<OpeningHourInput>? openingHours)
    {
        venue.Amenities = (amenities ?? []).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => new VenueAmenity { Name = x }).ToList();
        venue.Images = NormalizeImages(images).Select(x => new VenueImage
            { Url = x.Url.Trim(), SortOrder = x.SortOrder, IsPrimary = x.IsPrimary }).ToList();
        venue.OpeningHours = (openingHours ?? []).Select(x => new VenueOpeningHour
            { DayOfWeek = x.DayOfWeek, IsClosed = x.IsClosed, OpenTime = x.IsClosed ? null : x.OpenTime, CloseTime = x.IsClosed ? null : x.CloseTime }).ToList();
    }

    private static void AddCourtImages(Court court, IReadOnlyList<VenueImageInput>? images)
    {
        court.Images = NormalizeImages(images).Select(x => new CourtImage
            { Url = x.Url.Trim(), SortOrder = x.SortOrder, IsPrimary = x.IsPrimary }).ToList();
    }

    private static IReadOnlyList<VenueImageInput> NormalizeImages(IReadOnlyList<VenueImageInput>? images)
    {
        var result = (images ?? []).GroupBy(x => x.Url.Trim(), StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
        if (result.Count > 0 && result.All(x => !x.IsPrimary)) result[0] = result[0] with { IsPrimary = true };
        if (result.Count(x => x.IsPrimary) > 1)
        {
            var firstPrimary = result.FindIndex(x => x.IsPrimary);
            for (var index = 0; index < result.Count; index++) result[index] = result[index] with { IsPrimary = index == firstPrimary };
        }
        return result;
    }

    private static CourtStatus ParseCourtStatus(string value) =>
        Enum.TryParse<CourtStatus>(value, true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new AppException("Court status is invalid.", 400);

    private static void ResetVenueApproval(Venue venue)
    {
        venue.Status = VenueStatus.Draft;
        venue.RejectionReason = null;
        venue.ReviewedAt = null;
        venue.ReviewedBy = null;
    }

    private Guid RequireUserId() => currentUserService.UserId ?? throw new AppException("Unauthorized.", 401);
}
