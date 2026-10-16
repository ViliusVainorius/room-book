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

public class ReservationsControllerTests
{
    private static (ReservationsController Controller, RoomBookDbContext Context)
        CreateSut()
    {
        var options = new DbContextOptionsBuilder<RoomBookDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RoomBookDbContext(options);

        context.Rooms.Add(new Room
        {
            Id = 1,
            Name = "Room A",
            Capacity = 4,
            Floor = "1",
            IsActive = true
        });

        context.Users.Add(new User
        {
            Id = 1,
            FirstName = "Jonas",
            LastName = "Jonaitis"
        });

        context.Reservations.Add(new Reservation
        {
            Id = 1,
            RoomId = 1,
            OrganizerId = 1,
            Title = "Team Meeting",
            StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            AttendeesCount = 3,
            Status = ReservationStatus.Booked
        });

        context.SaveChanges();

        var service = new ReservationService(
            context,
            Options.Create(new BookingOptions
            {
                MaxDurationDays = 7,
                MaxBookingHorizonDays = 90
            }),
            TimeProvider.System);

        var controller = new ReservationsController(context, service);

        return (controller, context);
    }

    private static async Task<List<ReservationDto>> GetReservationsAsync(
        ReservationsController controller,
        ReservationStatus? status = null)
    {
        var result = await controller.GetReservations(
            roomId: null,
            organizerId: null,
            status: status,
            startDate: null,
            endDate: null);

        var okResult = result.Result.Should()
            .BeOfType<OkObjectResult>().Subject;

        return okResult.Value.Should()
            .BeAssignableTo<IEnumerable<ReservationDto>>().Subject
            .ToList();
    }

    [Fact]
    public async Task GetReservations_ReturnsReservationsWithRoomAndOrganizerDetails()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var reservations = await GetReservationsAsync(controller);

            var reservation = reservations.Should().ContainSingle().Subject;

            reservation.RoomName.Should().Be("Room A");
            reservation.OrganizerFirstName.Should().Be("Jonas");
            reservation.OrganizerLastName.Should().Be("Jonaitis");
        }
    }

    [Fact]
    public async Task GetReservations_FilterByStatus_ReturnsOnlyMatching()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Cancelled Meeting",
                StartDateTime = DateTime.UtcNow.AddDays(2),
                EndDateTime = DateTime.UtcNow.AddDays(2).AddHours(1),
                AttendeesCount = 2,
                Status = ReservationStatus.Cancelled
            });

            await context.SaveChangesAsync();

            var booked = await GetReservationsAsync(
                controller, ReservationStatus.Booked);

            var cancelled = await GetReservationsAsync(
                controller, ReservationStatus.Cancelled);

            booked.Should().ContainSingle()
                .Which.Title.Should().Be("Team Meeting");
            cancelled.Should().ContainSingle()
                .Which.Title.Should().Be("Cancelled Meeting");
        }
    }

    [Fact]
    public async Task GetReservationById_Existing_ReturnsDtoWithDetails()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var result = await controller.GetReservationById(1);

            var okResult = result.Result.Should()
                .BeOfType<OkObjectResult>().Subject;

            var reservation = okResult.Value.Should()
                .BeOfType<ReservationDto>().Subject;

            reservation.RoomName.Should().Be("Room A");
            reservation.OrganizerFirstName.Should().Be("Jonas");
        }
    }

    [Fact]
    public async Task GetReservations_InvalidDateRange_ReturnsBadRequest()
    {
        var (controller, context) = CreateSut();
        using (context)
        {
            var start = DateTime.UtcNow.AddDays(2);
            var end = DateTime.UtcNow.AddDays(1);

            var result = await controller.GetReservations(
                roomId: null,
                organizerId: null,
                status: null,
                startDate: start,
                endDate: end);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
