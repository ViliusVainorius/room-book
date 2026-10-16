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

            var now = DateTime.Now;

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
                    RoomId = roomA.Id,
                    OrganizerId = austeja.Id,
                    Title = "Previous Team Meeting",
                    StartDateTime = now.AddDays(-3).Date.AddHours(10),
                    EndDateTime = now.AddDays(-3).Date.AddHours(11),
                    AttendeesCount = 3,
                    Status = ReservationStatus.Completed
                }
            };

            await context.Reservations.AddRangeAsync(reservations);
        }

        await context.SaveChangesAsync();
    }
}
