using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBook.Api.DTOs;
using RoomBook.Api.Resources;
using RoomBook.Api.Services;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController
    (RoomBookDbContext context, 
    ReservationService reservationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations(
        [FromQuery] int? roomId,
        [FromQuery] int? organizerId,
        [FromQuery] ReservationStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (startDate.HasValue &&
            endDate.HasValue &&
            startDate.Value > endDate.Value)
        {
            return BadRequest(new
            {
                Code = "INVALID_DATE_RANGE",
                Message = "Start date must be before or equal to end date."
            });
        }

        var query = context.Reservations.AsQueryable();

        if (roomId.HasValue)
        {
            query = query.Where(
                reservation => reservation.RoomId == roomId.Value);
        }

        if (organizerId.HasValue)
        {
            query = query.Where(
                reservation => reservation.OrganizerId == organizerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(
                reservation => reservation.Status == status.Value);
        }

        // Return reservations that overlap the requested date range.
        if (startDate.HasValue)
        {
            query = query.Where(
                reservation =>
                    reservation.EndDateTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(
                reservation =>
                    reservation.StartDateTime <= endDate.Value);
        }

        var reservations = await query
            .Select(reservation => new ReservationDto(
                reservation.Id,
                reservation.RoomId,
                reservation.Room.Name,
                reservation.OrganizerId,
                reservation.Organizer.FirstName,
                reservation.Organizer.LastName,
                reservation.Title,
                reservation.StartDateTime,
                reservation.EndDateTime,
                reservation.AttendeesCount,
                reservation.Status,
                reservation.ActualEndDateTime,
                reservation.ActualStartDateTime
            ))
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> CreateReservation(
    CreateReservationRequest request)
    {
        var result = await reservationService.CreateAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiErrorResponse(
                result.ErrorCode!,
                result.ErrorMessage!));
        }

        var reservation = result.Reservation!;

        var room = await context.Rooms.FindAsync(reservation.RoomId);
        var user = await context.Users.FindAsync(reservation.OrganizerId);

        var response = new ReservationDto(
            reservation.Id,
            reservation.RoomId,
            room!.Name,
            reservation.OrganizerId,
            user!.FirstName,
            user!.LastName,
            reservation.Title,
            reservation.StartDateTime,
            reservation.EndDateTime,
            reservation.AttendeesCount,
            reservation.Status,
            reservation.ActualEndDateTime,
            reservation.ActualStartDateTime);

        return CreatedAtAction(
            nameof(GetReservationById),
            new { id = reservation.Id },
            response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetReservationById(int id)
    {
        var reservation = await context.Reservations
            .Where(r => r.Id == id)
            .Select(r => new ReservationDto(
                r.Id,
                r.RoomId,
                r.Room.Name,
                r.OrganizerId,
                r.Organizer.FirstName,
                r.Organizer.LastName,
                r.Title,
                r.StartDateTime,
                r.EndDateTime,
                r.AttendeesCount,
                r.Status,
                r.ActualEndDateTime,
                r.ActualStartDateTime))
            .FirstOrDefaultAsync();

        if (reservation is null)
        {
            return NotFound(new ApiErrorResponse(
                "RESERVATION_NOT_FOUND",
                Strings.RESERVATION_NOT_FOUND));
        }

        return Ok(reservation);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<ReservationDto>> CancelReservation(int id)
    {
        var result = await reservationService.CancelAsync(id);

        if (!result.IsSuccess)
        {
            var error = new ApiErrorResponse(
                result.ErrorCode!,
                result.ErrorMessage!);

            if (result.ErrorCode == "RESERVATION_NOT_FOUND")
                return NotFound(error);

            return BadRequest(error);
        }

        return await GetReservationById(id);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<ActionResult<ReservationDto>> CompleteReservation(int id)
    {
        var result = await reservationService.CompleteAsync(id);

        if (!result.IsSuccess)
        {
            var error = new ApiErrorResponse(
                result.ErrorCode!,
                result.ErrorMessage!);

            if (result.ErrorCode == "RESERVATION_NOT_FOUND")
                return NotFound(error);

            return BadRequest(error);
        }

        return await GetReservationById(id);
    }

    [HttpPatch("{id:int}/start")]
    public async Task<ActionResult<ReservationDto>> StartReservation(int id)
    {
        var result = await reservationService.StartAsync(id);

        if (!result.IsSuccess)
        {
            var error = new ApiErrorResponse(
                result.ErrorCode!,
                result.ErrorMessage!);

            if (result.ErrorCode == "RESERVATION_NOT_FOUND")
                return NotFound(error);

            return BadRequest(error);
        }

        return await GetReservationById(id);
    }
}
