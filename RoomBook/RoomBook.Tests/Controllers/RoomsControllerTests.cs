using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomBook.Api.Configuration;
using RoomBook.Api.Controllers;
using RoomBook.Api.DTOs;
using RoomBook.Api.Services;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;
using Xunit;

namespace RoomBook.Unit.Tests.Controllers;

public class RoomsControllerTests
{
    private static (RoomsController Controller, RoomBookDbContext Context)
        CreateSut(int? defaultWindowDays = null)
    {
        var options = new DbContextOptionsBuilder<RoomBookDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RoomBookDbContext(options);

        // Mirrors the application seed data.
        context.Rooms.AddRange(
            new Room
            {
                Id = 1,
                Name = "Meeting Room A",
                Capacity = 4,
                Floor = "1",
                Equipment = new List<EquipmentType>
                {
                    EquipmentType.Whiteboard,
                    EquipmentType.TvScreen
                },
                IsActive = true
            },
            new Room
            {
                Id = 2,
                Name = "Conference Room B",
                Capacity = 12,
                Floor = "2",
                Equipment = new List<EquipmentType>
                {
                    EquipmentType.Projector,
                    EquipmentType.Whiteboard,
                    EquipmentType.VideoConferencing
                },
                IsActive = true
            },
            new Room
            {
                Id = 3,
                Name = "Meeting Room C",
                Capacity = 6,
                Floor = "3",
                Equipment = new List<EquipmentType>
                {
                    EquipmentType.TvScreen,
                    EquipmentType.VideoConferencing
                },
                IsActive = false
            });

        context.SaveChanges();

        var controller = new RoomsController(
            context,
            new RoomUtilizationService(context),
            new RoomUsageReportPdfService(),
            Options.Create(new UtilizationReportOptions
            {
                DefaultWindowDays = defaultWindowDays ?? 30
            }),
            TimeProvider.System);

        return (controller, context);
    }

    private static async Task<List<RoomDto>> GetRoomsAsync(
        RoomsController controller,
        int? minCapacity = null,
        string? floor = null,
        List<EquipmentType>? equipment = null)
    {
        var result = await controller.GetRooms(
            minCapacity,
            floor,
            equipment);

        var okResult = result.Result.Should()
            .BeOfType<OkObjectResult>().Subject;

        return okResult.Value.Should()
            .BeAssignableTo<IEnumerable<RoomDto>>().Subject
            .ToList();
    }

    [Fact]
    public async Task GetRooms_SingleEquipment_ReturnsRoomsWithThatEquipment()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var rooms = await GetRoomsAsync(
                controller,
                equipment: [EquipmentType.Whiteboard]);

            rooms.Should().Contain(room => room.Name == "Meeting Room A");
            rooms.Should().Contain(room => room.Name == "Conference Room B");
            rooms.Should().NotContain(room => room.Name == "Meeting Room C");
        }
    }

    [Fact]
    public async Task GetRooms_MultipleEquipment_ReturnsOnlyRoomsWithAllSelectedItems()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var rooms = await GetRoomsAsync(
                controller,
                equipment:
                [
                    EquipmentType.Whiteboard,
                    EquipmentType.Projector
                ]);

            rooms.Should().ContainSingle()
                .Which.Name.Should().Be("Conference Room B");
        }
    }

    [Fact]
    public async Task GetRooms_MultipleEquipment_WithoutMatch_ReturnsEmptyList()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var rooms = await GetRoomsAsync(
                controller,
                equipment:
                [
                    EquipmentType.Whiteboard,
                    EquipmentType.Projector,
                    EquipmentType.TvScreen
                ]);

            rooms.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetRooms_DuplicateEquipmentValues_AreTreatedAsOne()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var rooms = await GetRoomsAsync(
                controller,
                equipment:
                [
                    EquipmentType.Whiteboard,
                    EquipmentType.Whiteboard
                ]);

            rooms.Select(room => room.Name).Should().BeEquivalentTo(
                "Meeting Room A",
                "Conference Room B");
        }
    }

    [Fact]
    public async Task GetRooms_EquipmentCombinedWithMinCapacity_FiltersBoth()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            // Meeting Room A has a whiteboard but only 4 seats.
            var rooms = await GetRoomsAsync(
                controller,
                minCapacity: 5,
                equipment: [EquipmentType.Whiteboard]);

            rooms.Should().ContainSingle()
                .Which.Name.Should().Be("Conference Room B");
        }
    }

    [Fact]
    public async Task GetRooms_EquipmentCombinedWithFloor_FiltersBoth()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var rooms = await GetRoomsAsync(
                controller,
                floor: "1",
                equipment: [EquipmentType.Whiteboard]);

            rooms.Should().ContainSingle()
                .Which.Name.Should().Be("Meeting Room A");
        }
    }

    [Fact]
    public async Task GetRooms_NoFilters_ReturnsAllRoomsIncludingInactive()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var rooms = await GetRoomsAsync(controller);

            rooms.Should().HaveCount(3);
            rooms.Should().Contain(room =>
                room.Name == "Meeting Room C" && !room.IsActive);
        }
    }

    [Fact]
    public async Task GetUtilization_ValidRange_ReturnsReportForAllRooms()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;

            var result = await controller.GetUtilization(from, to);

            var okResult = result.Result.Should()
                .BeOfType<OkObjectResult>().Subject;

            var report = okResult.Value.Should()
                .BeAssignableTo<IReadOnlyList<RoomUtilizationDto>>().Subject;

            report.Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task GetUtilization_InvalidRange_ReturnsBadRequest()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var moment = DateTime.UtcNow;

            var result = await controller.GetUtilization(moment, moment);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }

    [Fact]
    public async Task GetUtilization_NoDateRange_UsesConfiguredDefaultWindow()
    {
        // A 7-day default window instead of the standard 30.
        var (controller, context) = CreateSut(defaultWindowDays: 7);
        using (context)
        {
            // Completed 10 days ago: outside the configured window.
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Old Meeting",
                StartDateTime = DateTime.UtcNow.AddDays(-10),
                EndDateTime = DateTime.UtcNow.AddDays(-10).AddHours(1),
                AttendeesCount = 3,
                Status = ReservationStatus.Completed
            });

            // Completed 2 days ago: inside the configured window.
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Recent Meeting",
                StartDateTime = DateTime.UtcNow.AddDays(-2),
                EndDateTime = DateTime.UtcNow.AddDays(-2).AddHours(1),
                AttendeesCount = 3,
                Status = ReservationStatus.Completed
            });

            await context.SaveChangesAsync();

            var result = await controller.GetUtilization(null, null);

            var okResult = result.Result.Should()
                .BeOfType<OkObjectResult>().Subject;

            var report = okResult.Value.Should()
                .BeAssignableTo<IReadOnlyList<RoomUtilizationDto>>().Subject;

            var roomA = report.Single(r => r.RoomId == 1);

            // Only the recent meeting is inside the 7-day window.
            roomA.ScheduledHours.Should().Be(1);
        }
    }

    [Fact]
    public async Task ExportUtilization_DefaultWindow_ReturnsPdfFile()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var result = await controller.ExportUtilization(null, null);

            var file = result.Should()
                .BeOfType<FileContentResult>().Subject;

            file.ContentType.Should().Be("application/pdf");
            file.FileDownloadName.Should().Be("room-usage-report.pdf");

            // A PDF document starts with the %PDF magic bytes.
            file.FileContents.Should().NotBeEmpty();
            file.FileContents.Take(4)
                .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
        }
    }

    [Fact]
    public async Task ExportUtilization_InvalidRange_ReturnsBadRequest()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var moment = DateTime.UtcNow;

            var result = await controller.ExportUtilization(moment, moment);

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
