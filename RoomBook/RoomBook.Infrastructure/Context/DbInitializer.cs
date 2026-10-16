using Microsoft.EntityFrameworkCore;
using RoomBook.Domain.Models;

namespace RoomBook.Infrastructure.Context;

public static class DbInitializer
{
    public static async Task SeedAsync(RoomBookDbContext context)
    {
        // Seed rooms
        if (!await context.Rooms.AnyAsync())
        {
            var rooms = new List<Room>
            {
                new()
                {
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
                new()
                {
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
                new()
                {
                    Name = "Meeting Room C",
                    Capacity = 6,
                    Floor = "3",
                    Equipment = new List<EquipmentType>
                    {
                        EquipmentType.TvScreen,
                        EquipmentType.VideoConferencing
                    },
                    IsActive = false
                }
            };

            await context.Rooms.AddRangeAsync(rooms);
        }

        // Seed users
        if (!await context.Users.AnyAsync())
        {
            var users = new List<User>
            {
                new() { FirstName = "Jonas", LastName = "Jonaitis" },
                new() { FirstName = "Austeja", LastName = "Petrauskaite" },
                new() { FirstName = "Mantas", LastName = "Kazlauskas" }
            };
            await context.Users.AddRangeAsync(users);
        }

        await context.SaveChangesAsync();

        // Seed reservations
        if (!await context.Reservations.AnyAsync())
        {
            var roomA = await context.Rooms
                .FirstAsync(r => r.Name == "Meeting Room A");

            var roomB = await context.Rooms
                .FirstAsync(r => r.Name == "Conference Room B");

            var jonas = await context.Users
                .FirstAsync(u => u.FirstName == "Jonas");

            var austeja = await context.Users
                .FirstAsync(u => u.FirstName == "Austeja");

            var now = DateTime.UtcNow;

            var reservations = new List<Reservation>
            {
                new()
                {
                    RoomId = roomA.Id,
                    OrganizerId = jonas.Id,
                    Title = "Sprint Planning",
                    StartDateTime = now.AddDays(1).Date.AddHours(10),
                    EndDateTime = now.AddDays(1).Date.AddHours(11),
                    AttendeesCount = 4,
                    Status = ReservationStatus.Booked
                },
                new()
                {
                    RoomId = roomB.Id,
                    OrganizerId = austeja.Id,
                    Title = "Project Review",
                    StartDateTime = now.AddDays(2).Date.AddHours(14),
                    EndDateTime = now.AddDays(2).Date.AddHours(15),
                    AttendeesCount = 8,
                    Status = ReservationStatus.Booked
                },
                new()
                {
                    // Ran 30 of its 60 scheduled minutes: the usage
                    // report shows 50% for this room.
                    RoomId = roomA.Id,
                    OrganizerId = austeja.Id,
                    Title = "Previous Team Meeting",
                    StartDateTime = now.AddDays(-3).Date.AddHours(10),
                    EndDateTime = now.AddDays(-3).Date.AddHours(11),
                    ActualStartDateTime = now.AddDays(-3).Date.AddHours(10),
                    ActualEndDateTime = now.AddDays(-3).Date.AddHours(10)
                        .AddMinutes(30),
                    AttendeesCount = 3,
                    Status = ReservationStatus.Completed
                },
                new()
                {
                    // Cancelled meetings do not block the room.
                    RoomId = roomB.Id,
                    OrganizerId = jonas.Id,
                    Title = "Cancelled Weekly Sync",
                    StartDateTime = now.AddDays(-2).Date.AddHours(13),
                    EndDateTime = now.AddDays(-2).Date.AddHours(14),
                    AttendeesCount = 5,
                    Status = ReservationStatus.Cancelled
                },
                new()
                {
                    // No-show: booked but never held, so the usage
                    // report shows booked time with zero actual use.
                    RoomId = roomB.Id,
                    OrganizerId = austeja.Id,
                    Title = "Never Held Meeting",
                    StartDateTime = now.AddDays(-1).Date.AddHours(15),
                    EndDateTime = now.AddDays(-1).Date.AddHours(16),
                    AttendeesCount = 4,
                    Status = ReservationStatus.Booked
                }
            };

            await context.Reservations.AddRangeAsync(reservations);
        }

        await context.SaveChangesAsync();
    }
}
