using FluentAssertions;
using RoomBook.Api.DTOs;
using RoomBook.Api.Services;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;
using Xunit;
using static RoomBook.Unit.Tests.Testing.ReservationTestData;

namespace RoomBook.Unit.Tests.Services;

public class RoomUtilizationTests
{
    private static async Task<List<RoomUtilizationDto>> GetReportAsync(
        RoomBookDbContext context,
        DateTime? from = null,
        DateTime? to = null)
    {
        var service = new RoomUtilizationService(context);

        var report = await service.GetUtilizationAsync(
            from ?? Now.AddDays(-1),
            to ?? Now);

        return report.ToList();
    }

    [Fact]
    public async Task GetUtilizationAsync_CompletedMeeting_CountsScheduledAndActualHours()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Done Meeting",
                StartDateTime = Now.AddHours(-3),
                EndDateTime = Now.AddHours(-2),
                ActualStartDateTime = Now.AddHours(-3),
                ActualEndDateTime = Now.AddHours(-3).AddMinutes(20),
                AttendeesCount = 3,
                Status = ReservationStatus.Completed
            });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);
            var room = report.Single(r => r.RoomId == 1);

            room.ReservationCount.Should().Be(1);
            room.CompletedReservationsCount.Should().Be(1);
            room.CancelledReservationsCount.Should().Be(0);
            room.ScheduledHours.Should().Be(1);
            room.ActualHours.Should().Be(0.33);
            room.UsagePercentage.Should().Be(33.33);
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_HoursClippedToReportingWindow()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Long Meeting",
                StartDateTime = Now.AddDays(-2),
                EndDateTime = Now.AddHours(-1),
                ActualStartDateTime = Now.AddDays(-2),
                ActualEndDateTime = Now.AddHours(-1),
                AttendeesCount = 3,
                Status = ReservationStatus.Completed
            });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);
            var room = report.Single(r => r.RoomId == 1);

            room.ScheduledHours.Should().Be(23);
            room.ActualHours.Should().Be(23);
            room.UsagePercentage.Should().Be(100);
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_MeetingOutsideWindow_IsNotCounted()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Old Meeting",
                StartDateTime = Now.AddDays(-5),
                EndDateTime = Now.AddDays(-4),
                ActualStartDateTime = Now.AddDays(-5),
                ActualEndDateTime = Now.AddDays(-5).AddHours(1),
                AttendeesCount = 3,
                Status = ReservationStatus.Completed
            });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);
            var room = report.Single(r => r.RoomId == 1);

            room.ReservationCount.Should().Be(0);
            room.ScheduledHours.Should().Be(0);
            room.ActualHours.Should().Be(0);
            room.UsagePercentage.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_CompletedWithoutActualDates_CountsScheduledHours()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Closed Without Actuals",
                StartDateTime = Now.AddHours(-2),
                EndDateTime = Now.AddHours(-1),
                AttendeesCount = 3,
                Status = ReservationStatus.Completed
            });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);
            var room = report.Single(r => r.RoomId == 1);

            room.ScheduledHours.Should().Be(1);
            room.ActualHours.Should().Be(1);
            room.UsagePercentage.Should().Be(100);
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_CompletedWithOnlyOneActualDate_CountsScheduledHours()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Half Recorded",
                StartDateTime = Now.AddHours(-2),
                EndDateTime = Now.AddHours(-1),
                ActualStartDateTime = Now.AddHours(-2),
                AttendeesCount = 3,
                Status = ReservationStatus.Completed
            });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);
            var room = report.Single(r => r.RoomId == 1);

            room.ScheduledHours.Should().Be(1);
            room.ActualHours.Should().Be(1);
            room.UsagePercentage.Should().Be(100);
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_BookedButNeverHeld_CountsWithZeroUsage()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "No-show Meeting",
                StartDateTime = Now.AddHours(-2),
                EndDateTime = Now.AddHours(-1),
                AttendeesCount = 3,
                Status = ReservationStatus.Booked
            });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);
            var room = report.Single(r => r.RoomId == 1);

            room.ReservationCount.Should().Be(1);
            room.CompletedReservationsCount.Should().Be(0);
            room.ScheduledHours.Should().Be(0);
            room.ActualHours.Should().Be(0);
            room.UsagePercentage.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_CancelledMeeting_ExcludedFromCountsAndHours()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Cancelled Meeting",
                StartDateTime = Now.AddHours(-2),
                EndDateTime = Now.AddHours(-1),
                AttendeesCount = 3,
                Status = ReservationStatus.Cancelled
            });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);
            var room = report.Single(r => r.RoomId == 1);

            room.ReservationCount.Should().Be(0);
            room.CancelledReservationsCount.Should().Be(1);
            room.ScheduledHours.Should().Be(0);
            room.ActualHours.Should().Be(0);
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_MultipleRooms_AggregatesPerRoom()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            context.Reservations.AddRange(
                new Reservation
                {
                    RoomId = 1,
                    OrganizerId = 1,
                    Title = "Partially Used",
                    StartDateTime = Now.AddHours(-3),
                    EndDateTime = Now.AddHours(-2),
                    ActualStartDateTime = Now.AddHours(-3),
                    ActualEndDateTime = Now.AddHours(-3).AddMinutes(20),
                    AttendeesCount = 3,
                    Status = ReservationStatus.Completed
                },
                new Reservation
                {
                    RoomId = 2,
                    OrganizerId = 2,
                    Title = "Fully Used",
                    StartDateTime = Now.AddHours(-3),
                    EndDateTime = Now.AddHours(-1),
                    ActualStartDateTime = Now.AddHours(-3),
                    ActualEndDateTime = Now.AddHours(-1),
                    AttendeesCount = 5,
                    Status = ReservationStatus.Completed
                });

            await context.SaveChangesAsync();

            var report = await GetReportAsync(context);

            var roomA = report.Single(r => r.RoomId == 1);
            var roomB = report.Single(r => r.RoomId == 2);

            roomA.ScheduledHours.Should().Be(1);
            roomA.UsagePercentage.Should().Be(33.33);

            roomB.ScheduledHours.Should().Be(2);
            roomB.ActualHours.Should().Be(2);
            roomB.UsagePercentage.Should().Be(100);
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_InactiveRoom_IsStillReported()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            var report = await GetReportAsync(context);

            report.Should().HaveCount(3);
            report.Single(r => r.RoomId == 3).IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetUtilizationAsync_InvalidRange_Throws()
    {
        var (context, _) = CreateSut();
        using (context)
        {
            var service = new RoomUtilizationService(context);

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetUtilizationAsync(Now, Now));
        }
    }
}
