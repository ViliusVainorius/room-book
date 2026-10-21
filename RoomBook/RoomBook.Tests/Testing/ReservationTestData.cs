using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomBook.Api.Configuration;
using RoomBook.Api.DTOs;
using RoomBook.Api.Services;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;

namespace RoomBook.Unit.Tests.Testing;

internal static class ReservationTestData
{
    public static readonly DateTime Now =
        new(2026, 10, 16, 9, 0, 0, DateTimeKind.Utc);

    public static (RoomBookDbContext Context, ReservationService Service)
        CreateSut()
    {
        var options = new DbContextOptionsBuilder<RoomBookDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RoomBookDbContext(options);

        context.Rooms.AddRange(
            new Room
            {
                Id = 1,
                Name = "Room A",
                Capacity = 4,
                Floor = "1",
                IsActive = true
            },
            new Room
            {
                Id = 2,
                Name = "Room B",
                Capacity = 10,
                Floor = "2",
                IsActive = true
            },
            new Room
            {
                Id = 3,
                Name = "Room C",
                Capacity = 8,
                Floor = "3",
                IsActive = false
            });

        context.Users.AddRange(
            new User
            {
                Id = 1,
                FirstName = "Jonas",
                LastName = "Jonaitis"
            },
            new User
            {
                Id = 2,
                FirstName = "Austeja",
                LastName = "Petrauskaite"
            });

        context.SaveChanges();

        var bookingOptions = Options.Create(new BookingOptions
        {
            MaxDurationDays = 7,
            MaxBookingHorizonDays = 90
        });

        var timeProvider = new FixedTimeProvider(Now);

        var service = new ReservationService(
            context,
            bookingOptions,
            timeProvider);

        return (context, service);
    }

    public static CreateReservationRequest ValidRequest() =>
        new(
            RoomId: 1,
            OrganizerId: 1,
            Title: "Team Meeting",
            StartDateTime: Now.AddDays(1),
            EndDateTime: Now.AddDays(1).AddHours(1),
            AttendeesCount: 3);
}
