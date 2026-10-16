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

    public async Task<ReservationCreationResult> CreateAsync(
        CreateReservationRequest request)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Validate the time range.
        if (request.EndDateTime <= request.StartDateTime)
        {
            return ReservationCreationResult.Failure(
                "INVALID_TIME_RANGE",
                Strings.INVALID_TIME_RANGE);
        }

        // R4: Start must be in the future.
        if (request.StartDateTime <= now)
        {
            return ReservationCreationResult.Failure(
                "START_NOT_IN_FUTURE",
                Strings.START_NOT_IN_FUTURE);
        }

        // R1: Maximum reservation duration.
        if (request.EndDateTime - request.StartDateTime >
            TimeSpan.FromDays(_options.MaxDurationDays))
        {
            return ReservationCreationResult.Failure(
                "MAX_DURATION_EXCEEDED",
                string.Format(Strings.MAX_DURATION_EXCEEDED, _options.MaxDurationDays));
        }

        // R6: Booking horizon.
        if (request.StartDateTime >
            now.AddDays(_options.MaxBookingHorizonDays))
        {
            return ReservationCreationResult.Failure(
                "BOOKING_HORIZON_EXCEEDED",
                string.Format(Strings.BOOKING_HORIZON_EXCEEDED, _options.MaxBookingHorizonDays));
        }

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == request.RoomId);

        if (room is null)
        {
            return ReservationCreationResult.Failure(
                "ROOM_NOT_FOUND",
                Strings.ROOM_NOT_FOUND);
        }

        var organizerExists = await _context.Users
            .AnyAsync(u => u.Id == request.OrganizerId);

        if (!organizerExists)
        {
            return ReservationCreationResult.Failure(
                "ORGANIZER_NOT_FOUND",
                Strings.ORGANIZER_NOT_FOUND);
        }

        // R7: Only active rooms can be booked.
        if (!room.IsActive)
        {
            return ReservationCreationResult.Failure(
                "ROOM_INACTIVE",
                Strings.ROOM_INACTIVE);
        }

        // R5: The room must have sufficient capacity.
        if (request.AttendeesCount > room.Capacity)
        {
            return ReservationCreationResult.Failure(
                "ROOM_CAPACITY_EXCEEDED",
                string.Format(Strings.ROOM_CAPACITY_EXCEEDED, room.Capacity));
        }

        // R2: A room cannot have overlapping booked reservations.
        var roomConflict = await _context.Reservations.AnyAsync(r =>
            r.RoomId == request.RoomId &&
            r.Status == ReservationStatus.Booked &&
            r.StartDateTime < request.EndDateTime &&
            r.EndDateTime > request.StartDateTime);

        if (roomConflict)
        {
            return ReservationCreationResult.Failure(
                "ROOM_BOOKING_CONFLICT",
                Strings.ROOM_BOOKING_CONFLICT);
        }

        // R3: An organizer cannot organize overlapping meetings.
        var organizerConflict = await _context.Reservations.AnyAsync(r =>
            r.OrganizerId == request.OrganizerId &&
            r.Status == ReservationStatus.Booked &&
            r.StartDateTime < request.EndDateTime &&
            r.EndDateTime > request.StartDateTime);

        if (organizerConflict)
        {
            return ReservationCreationResult.Failure(
                "ORGANIZER_BOOKING_CONFLICT",
                Strings.ORGANIZER_BOOKING_CONFLICT);
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

        return ReservationCreationResult.Success(reservation);
    }

    public async Task<ReservationOperationResult> CancelAsync(int id)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return ReservationOperationResult.Failure(
                "RESERVATION_NOT_FOUND",
                Strings.RESERVATION_NOT_FOUND);
        }

        if (reservation.Status != ReservationStatus.Booked)
        {
            return ReservationOperationResult.Failure(
                "CANCEL_RESERVATION_NOT_BOOKED",
                Strings.CANCEL_RESERVATION_NOT_BOOKED);
        }

        reservation.Status = ReservationStatus.Cancelled;

        await _context.SaveChangesAsync();

        return ReservationOperationResult.Success(reservation);
    }

    public async Task<ReservationOperationResult> CompleteAsync(int id)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return ReservationOperationResult.Failure(
                "RESERVATION_NOT_FOUND",
                Strings.RESERVATION_NOT_FOUND);
        }

        if (reservation.Status != ReservationStatus.Booked)
        {
            return ReservationOperationResult.Failure(
                "COMPLETE_RESERVATION_NOT_BOOKED",
                Strings.COMPLETE_RESERVATION_NOT_BOOKED);
        }

        if (!reservation.ActualStartDateTime.HasValue)
        {
            return ReservationOperationResult.Failure(
                "COMPLETE_MEETING_NOT_STARTED",
                Strings.COMPLETE_MEETING_NOT_STARTED);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now < reservation.StartDateTime)
        {
            return ReservationOperationResult.Failure(
                "MEETING_NOT_STARTED",
                Strings.MEETING_NOT_STARTED);
        }

        reservation.ActualEndDateTime = now;
        reservation.Status = ReservationStatus.Completed;

        await _context.SaveChangesAsync();

        return ReservationOperationResult.Success(reservation);
    }

    public async Task<ReservationOperationResult> StartAsync(int id)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return ReservationOperationResult.Failure(
                "RESERVATION_NOT_FOUND",
                Strings.RESERVATION_NOT_FOUND);
        }

        if (reservation.Status != ReservationStatus.Booked)
        {
            return ReservationOperationResult.Failure(
                "START_RESERVATION_NOT_BOOKED",
                Strings.START_RESERVATION_NOT_BOOKED);
        }

        if (reservation.ActualStartDateTime.HasValue)
        {
            return ReservationOperationResult.Failure(
                "MEETING_ALREADY_STARTED",
                Strings.MEETING_ALREADY_STARTED);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now < reservation.StartDateTime)
        {
            return ReservationOperationResult.Failure(
                "START_MEETING_NOT_STARTED",
                Strings.START_MEETING_NOT_STARTED);
        }

        if (now >= reservation.EndDateTime)
        {
            return ReservationOperationResult.Failure(
                "START_MEETING_TIME_EXPIRED",
                Strings.START_MEETING_TIME_EXPIRED);
        }

        reservation.ActualStartDateTime = now;

        await _context.SaveChangesAsync();

        return ReservationOperationResult.Success(reservation);
    }
}
