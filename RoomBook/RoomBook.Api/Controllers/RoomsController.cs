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
public class RoomsController(RoomBookDbContext context, RoomUtilizationService utilizationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms(
        [FromQuery] int? minCapacity,
        [FromQuery] string? floor,
        [FromQuery] List<EquipmentType>? equipment)
    {
        var query = context.Rooms.AsQueryable();

        if (minCapacity.HasValue)
        {
            query = query.Where(
                room => room.Capacity >= minCapacity.Value);
        }

        if (!string.IsNullOrWhiteSpace(floor))
        {
            query = query.Where(
                room => room.Floor == floor);
        }

        if (equipment is { Count: > 0 })
        {
            // A room must provide every selected equipment item,
            // e.g. ?equipment=Projector&equipment=VideoConferencing.
            // One filter per item keeps the expression translatable
            // by EF (All(...) over a collection property is not).
            foreach (var requiredItem in equipment.Distinct())
            {
                var item = requiredItem;

                query = query.Where(
                    room => room.Equipment.Contains(item));
            }
        }

        var rooms = await query
            .Select(room => new RoomDto(
                room.Id,
                room.Name,
                room.Capacity,
                room.Floor,
                room.Equipment,
                room.IsActive))
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("utilization")]
    public async Task<ActionResult<IReadOnlyList<RoomUtilizationDto>>>
    GetUtilization(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var reportEnd = to ?? DateTime.UtcNow;
        var reportStart = from ?? reportEnd.AddDays(-30);

        if (reportStart >= reportEnd)
        {
            return BadRequest(new ApiErrorResponse(
                "INVALID_DATE_RANGE",
                Strings.INVALID_DATE_RANGE));
        }

        var result = await utilizationService.GetUtilizationAsync(
            reportStart,
            reportEnd);

        return Ok(result);
    }
}
