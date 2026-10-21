using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomBook.Api.Configuration;
using RoomBook.Api.DTOs;
using RoomBook.Api.Resources;
using RoomBook.Api.Services;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(
    RoomBookDbContext context,
    RoomUtilizationService utilizationService,
    RoomUsageReportPdfService usageReportPdfService,
    IOptions<UtilizationReportOptions> reportOptions,
    TimeProvider timeProvider) : ControllerBase
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
    }    [HttpGet("utilization")]
    public async Task<ActionResult<IReadOnlyList<RoomUtilizationDto>>>
        GetUtilization(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
    {
        var window = ResolveReportWindow(from, to);

        if (window is null)
        {
            return BadRequest(new ApiErrorResponse(
                "INVALID_DATE_RANGE",
                Strings.INVALID_DATE_RANGE));
        }

        var (reportStart, reportEnd) = window.Value;

        var result = await utilizationService.GetUtilizationAsync(
            reportStart,
            reportEnd);

        return Ok(result);
    }

    [HttpGet("utilization/export")]
    public async Task<IActionResult> ExportUtilization(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var window = ResolveReportWindow(from, to);

        if (window is null)
        {
            return BadRequest(new ApiErrorResponse(
                "INVALID_DATE_RANGE",
                Strings.INVALID_DATE_RANGE));
        }

        var (reportStart, reportEnd) = window.Value;

        var report = await utilizationService.GetUtilizationAsync(
            reportStart,
            reportEnd);

        var pdf = usageReportPdfService.Generate(
            report,
            reportStart,
            reportEnd,
            timeProvider.GetUtcNow().UtcDateTime);

        return File(pdf, "application/pdf", "room-usage-report.pdf");
    }

    private (DateTime Start, DateTime End)? ResolveReportWindow(
        DateTime? from,
        DateTime? to)
    {
        var reportEnd = to ?? timeProvider.GetUtcNow().UtcDateTime;
        var reportStart = from ??
            reportEnd.AddDays(-reportOptions.Value.DefaultWindowDays);

        return reportStart < reportEnd ? (reportStart, reportEnd) : null;
    }
}
