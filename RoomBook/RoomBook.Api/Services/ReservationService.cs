using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomBook.Api.Configuration;
using RoomBook.Api.DTOs;
using RoomBook.Api.Resources;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;

namespace RoomBook.Api.Services;

public class ReservationService
{
    private readonly RoomBookDbContext _context;
    private readonly BookingOptions _options;
    private readonly TimeProvider _timeProvider;

    public ReservationService(
        RoomBookDbContext context,
        IOptions<BookingOptions> options,
        TimeProvider timeProvider)
    {
        _context = context;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<ReservationResult> CreateAsync(
        CreateReservationRequest request)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var timingError = ValidateTiming(request, now);
        if (timingError is not null)
        {
            return timingError;
        }

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == request.RoomId);

        if (room is null)
        {
            return ReservationResult.Failure(
                "ROOM_NOT_FOUND",
                Strings.ROOM_NOT_FOUND);
        }

        var organizerExists = await _context.Users
            .AnyAsync(u => u.Id == request.OrganizerId);

        if (!organizerExists)
        {
            return ReservationResult.Failure(
                "ORGANIZER_NOT_FOUND",
                Strings.ORGANIZER_NOT_FOUND);
        }

        var eligibilityError = ValidateRoomEligibility(room, request);
        if (eligibilityError is not null)
        {
            return eligibilityError;
        }

        var conflictError = await FindConflictAsync(request);
        if (conflictError is not null)
        {
            return conflictError;
        }

        var reservation = new Reservation
        {
            RoomId = room.Id,
            OrganizerId = request.OrganizerId,
            Title = request.Title.Trim(),
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            AttendeesCount = request.AttendeesCount,
            Status = ReservationStatus.Booked
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return ReservationResult.Success(reservation);
    }

    public async Task<ReservationResult> CancelAsync(int id)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return ReservationResult.Failure(
                "RESERVATION_NOT_FOUND",
                Strings.RESERVATION_NOT_FOUND);
        }

        if (reservation.Status != ReservationStatus.Booked)
        {
            return ReservationResult.Failure(
                "CANCEL_RESERVATION_NOT_BOOKED",
                Strings.CANCEL_RESERVATION_NOT_BOOKED);
        }

        reservation.Status = ReservationStatus.Cancelled;

        await _context.SaveChangesAsync();

        return ReservationResult.Success(reservation);
    }

    public async Task<ReservationResult> CompleteAsync(int id)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return ReservationResult.Failure(
                "RESERVATION_NOT_FOUND",
                Strings.RESERVATION_NOT_FOUND);
        }

        if (reservation.Status != ReservationStatus.Booked)
        {
            return ReservationResult.Failure(
                "COMPLETE_RESERVATION_NOT_BOOKED",
                Strings.COMPLETE_RESERVATION_NOT_BOOKED);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (reservation.ActualStartDateTime.HasValue)
        {
            if (now < reservation.ActualStartDateTime.Value)
            {
                return ReservationResult.Failure(
                    "MEETING_NOT_STARTED",
                    Strings.MEETING_NOT_STARTED);
            }
        }
        else
        {
            if (now <= reservation.StartDateTime)
            {
                return ReservationResult.Failure(
                    "COMPLETE_MEETING_NOT_STARTED",
                    Strings.COMPLETE_MEETING_NOT_STARTED);
            }

            reservation.ActualStartDateTime = reservation.StartDateTime;
        }

        reservation.ActualEndDateTime =
            now < reservation.EndDateTime
                ? now
                : reservation.EndDateTime;

        reservation.Status = ReservationStatus.Completed;

        await _context.SaveChangesAsync();

        return ReservationResult.Success(reservation);
    }

    // R1 maximum duration, R4 valid time range, R6 booking horizon.
    private ReservationResult? ValidateTiming(
        CreateReservationRequest request,
        DateTime now)
    {
        if (request.EndDateTime <= request.StartDateTime)
        {
            return ReservationResult.Failure(
                "INVALID_TIME_RANGE",
                Strings.INVALID_TIME_RANGE);
        }

        if (request.StartDateTime <= now)
        {
            return ReservationResult.Failure(
                "START_NOT_IN_FUTURE",
                Strings.START_NOT_IN_FUTURE);
        }

        if (request.EndDateTime - request.StartDateTime >
            TimeSpan.FromDays(_options.MaxDurationDays))
        {
            return ReservationResult.Failure(
                "MAX_DURATION_EXCEEDED",
                string.Format(
                    Strings.MAX_DURATION_EXCEEDED,
                    _options.MaxDurationDays));
        }

        if (request.StartDateTime >
            now.AddDays(_options.MaxBookingHorizonDays))
        {
            return ReservationResult.Failure(
                "BOOKING_HORIZON_EXCEEDED",
                string.Format(
                    Strings.BOOKING_HORIZON_EXCEEDED,
                    _options.MaxBookingHorizonDays));
        }

        return null;
    }

    // R5 capacity, R7 active rooms only.
    private static ReservationResult? ValidateRoomEligibility(
        Room room,
        CreateReservationRequest request)
    {
        if (!room.IsActive)
        {
            return ReservationResult.Failure(
                "ROOM_INACTIVE",
                Strings.ROOM_INACTIVE);
        }

        if (request.AttendeesCount > room.Capacity)
        {
            return ReservationResult.Failure(
                "ROOM_CAPACITY_EXCEEDED",
                string.Format(
                    Strings.ROOM_CAPACITY_EXCEEDED,
                    room.Capacity));
        }

        return null;
    }

    // R2 no overlapping room bookings, R3 no overlapping organizer
    // bookings. Cancelled reservations block neither.
    private async Task<ReservationResult?> FindConflictAsync(
        CreateReservationRequest request)
    {
        var roomConflict = await _context.Reservations.AnyAsync(r =>
            r.RoomId == request.RoomId &&
            r.Status == ReservationStatus.Booked &&
            r.StartDateTime < request.EndDateTime &&
            r.EndDateTime > request.StartDateTime);

        if (roomConflict)
        {
            return ReservationResult.Failure(
                "ROOM_BOOKING_CONFLICT",
                Strings.ROOM_BOOKING_CONFLICT);
        }

        var organizerConflict = await _context.Reservations.AnyAsync(r =>
            r.OrganizerId == request.OrganizerId &&
            r.Status == ReservationStatus.Booked &&
            r.StartDateTime < request.EndDateTime &&
            r.EndDateTime > request.StartDateTime);

        if (organizerConflict)
        {
            return ReservationResult.Failure(
                "ORGANIZER_BOOKING_CONFLICT",
                Strings.ORGANIZER_BOOKING_CONFLICT);
        }

        return null;
    }
}
