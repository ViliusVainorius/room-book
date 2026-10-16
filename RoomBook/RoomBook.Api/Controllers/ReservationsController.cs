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
            return BadRequest(new ApiErrorResponse(
                "INVALID_DATE_RANGE",
                "Start date must be before or equal to end date."));
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
                reservation => reservation.StartDateTime <= endDate.Value);
        }

        var reservations = await WithDetails(query)
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
            return Failure(result);
        }

        var reservationId = result.Reservation!.Id;
        var response = await FindDtoAsync(reservationId);

        return CreatedAtAction(
            nameof(GetReservationById),
            new { id = reservationId },
            response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetReservationById(int id)
    {
        var reservation = await FindDtoAsync(id);

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

        return result.IsSuccess
            ? await GetReservationById(id)
            : Failure(result);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<ActionResult<ReservationDto>> CompleteReservation(int id)
    {
        var result = await reservationService.CompleteAsync(id);

        return result.IsSuccess
            ? await GetReservationById(id)
            : Failure(result);
    }

    [HttpPatch("{id:int}/start")]
    public async Task<ActionResult<ReservationDto>> StartReservation(int id)
    {
        var result = await reservationService.StartAsync(id);

        return result.IsSuccess
            ? await GetReservationById(id)
            : Failure(result);
    }

    private ActionResult Failure(ReservationResult result)
    {
        var error = new ApiErrorResponse(
            result.ErrorCode!,
            result.ErrorMessage!);

        return result.ErrorCode == "RESERVATION_NOT_FOUND"
            ? NotFound(error)
            : BadRequest(error);
    }

    // The projection must stay inside the IQueryable so EF can
    // translate it; calling a helper on the materialized entity would
    // hit null Room/Organizer navigations.
    private IQueryable<ReservationDto> WithDetails(
        IQueryable<Reservation> reservations) =>
        reservations.Select(reservation => new ReservationDto(
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
            reservation.ActualStartDateTime));

    private async Task<ReservationDto?> FindDtoAsync(int id) =>
        await WithDetails(
                context.Reservations.Where(
                    reservation => reservation.Id == id))
            .FirstOrDefaultAsync();
}
