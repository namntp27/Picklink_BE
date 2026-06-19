using Picklink.Application.DTOs;
using Picklink.Domain.Entities;

namespace Picklink.Api.Services;

internal static class VenueApiMappings
{
    public static VenueListItemResponse ToListItem(Venue venue) => new(
        venue.Id,
        venue.Name,
        venue.Province!.Name,
        venue.Ward!.Name,
        venue.StreetAddress,
        venue.Latitude,
        venue.Longitude,
        venue.Status.ToString(),
        venue.Courts.Count(x => !x.IsDeleted),
        venue.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault());

    public static VenueDetailResponse ToDetail(Venue venue) => new(
        venue.Id,
        venue.OwnerId,
        venue.Name,
        venue.Description,
        venue.StreetAddress,
        venue.ProvinceId,
        venue.Province!.Name,
        venue.WardId,
        venue.Ward!.Name,
        venue.PhoneNumber,
        venue.Latitude,
        venue.Longitude,
        venue.Status.ToString(),
        venue.RejectionReason,
        venue.Amenities.OrderBy(x => x.Name).Select(x => x.Name).ToArray(),
        venue.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(ToImage).ToArray(),
        venue.OpeningHours.OrderBy(x => x.DayOfWeek).Select(ToOpeningHour).ToArray(),
        venue.Courts.Where(x => !x.IsDeleted).OrderBy(x => x.Code).Select(ToCourtSummary).ToArray());

    public static CourtListItemResponse ToCourtListItem(Court court) => new(
        court.Id,
        court.VenueId,
        court.Name,
        court.Code,
        court.Venue!.Name,
        court.Venue.Province!.Name,
        court.Venue.Ward!.Name,
        court.PricePerHour,
        court.SlotDurationMinutes,
        court.Status.ToString(),
        court.Venue.Latitude,
        court.Venue.Longitude,
        court.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault()
            ?? court.Venue.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault());

    public static CourtDetailResponse ToCourtDetail(Court court) => new(
        court.Id,
        court.VenueId,
        court.Name,
        court.Code,
        court.Venue!.Name,
        court.Venue.StreetAddress,
        court.Venue.Province!.Name,
        court.Venue.Ward!.Name,
        court.Venue.Latitude,
        court.Venue.Longitude,
        court.PricePerHour,
        court.SlotDurationMinutes,
        court.Status.ToString(),
        court.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(ToImage).ToArray(),
        court.Venue.OpeningHours.OrderBy(x => x.DayOfWeek).Select(ToOpeningHour).ToArray(),
        court.BlockedSlots.Where(x => !x.IsDeleted).OrderBy(x => x.StartTime)
            .Select(x => new BlockedSlotResponse(x.Id, x.StartTime, x.EndTime, x.Reason)).ToArray());

    private static CourtSummaryResponse ToCourtSummary(Court court) => new(
        court.Id,
        court.Name,
        court.Code,
        court.PricePerHour,
        court.SlotDurationMinutes,
        court.Status.ToString(),
        court.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault());

    private static VenueImageResponse ToImage(VenueImage image) =>
        new(image.Id, image.Url, image.SortOrder, image.IsPrimary);

    private static VenueImageResponse ToImage(CourtImage image) =>
        new(image.Id, image.Url, image.SortOrder, image.IsPrimary);

    private static OpeningHourResponse ToOpeningHour(VenueOpeningHour item) =>
        new(item.DayOfWeek, item.IsClosed, item.OpenTime, item.CloseTime);
}
