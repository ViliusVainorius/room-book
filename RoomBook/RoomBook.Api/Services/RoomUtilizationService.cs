using Microsoft.EntityFrameworkCore;
using RoomBook.Api.DTOs;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;

namespace RoomBook.Api.Services;

public class RoomUtilizationService(RoomBookDbContext context)
{
    public async Task<IReadOnlyList<RoomUtilizationDto>> GetUtilizationAsync(
        DateTime from,
        DateTime to)
    {
        if (from >= to)
        {
            throw new ArgumentException(
                "'from' must be earlier than 'to'.");
        }

        var rooms = await context.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Id)
            .ToListAsync();

        // Retrieve reservations whose scheduled intervals overlap
        // the reporting period.
        var reservations = await context.Reservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.StartDateTime < to &&
                reservation.EndDateTime > from)
            .ToListAsync();

        var results = new List<RoomUtilizationDto>();

        foreach (var room in rooms)
        {
            var roomReservations = reservations
                .Where(reservation => reservation.RoomId == room.Id)
                .ToList();

            var completedReservations = roomReservations
                .Where(reservation =>
                    reservation.Status == ReservationStatus.Completed)
                .ToList();

            var cancelledReservations = roomReservations
                .Where(reservation =>
                    reservation.Status == ReservationStatus.Cancelled)
                .ToList();

            // Compare scheduled and actual time for completed meetings only.
            var scheduledHours = completedReservations.Sum(reservation =>
                GetOverlappingHours(
                    reservation.StartDateTime,
                    reservation.EndDateTime,
                    from,
                    to));

            var actualHours = completedReservations.Sum(reservation =>
            {
                if (reservation.ActualStartDateTime is not DateTime actualStart ||
                    reservation.ActualEndDateTime is not DateTime actualEnd ||
                    actualEnd <= actualStart)
                {
                    return 0d;
                }

                return GetOverlappingHours(
                    actualStart,
                    actualEnd,
                    from,
                    to);
            });

            double? usagePercentage = scheduledHours > 0
                ? actualHours / scheduledHours * 100
                : null;

            results.Add(new RoomUtilizationDto(
                room.Id,
                room.Name,
                room.Floor,
                room.IsActive,
                roomReservations.Count(reservation =>
                    reservation.Status != ReservationStatus.Cancelled),
                completedReservations.Count,
                cancelledReservations.Count,
                Math.Round(scheduledHours, 2),
                Math.Round(actualHours, 2),
                usagePercentage.HasValue
                    ? Math.Round(usagePercentage.Value, 2)
                    : null
            ));
        }

        return results;
    }

    private static double GetOverlappingHours(
        DateTime start,
        DateTime end,
        DateTime from,
        DateTime to)
    {
        var clippedStart = start > from ? start : from;
        var clippedEnd = end < to ? end : to;

        if (clippedEnd <= clippedStart)
        {
            return 0;
        }

        return (clippedEnd - clippedStart).TotalHours;
    }
}
