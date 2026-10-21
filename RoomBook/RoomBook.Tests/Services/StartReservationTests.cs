using FluentAssertions;
using RoomBook.Domain.Models;
using Xunit;
using static RoomBook.Unit.Tests.Testing.ReservationTestData;

namespace RoomBook.Unit.Tests.Services;

public class StartReservationTests
{
    [Fact]
    public async Task StartAsync_WhenMeetingIsDue_StartsMeetingAndRecordsActualStartTime()
    {
        // Arrange
        var (context, service) = CreateSut();
        using (context)
        {
            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Team meeting",
                StartDateTime = Now,
                EndDateTime = Now.AddHours(1),
                AttendeesCount = 3,
                Status = ReservationStatus.Booked
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            // Act
            var result = await service.StartAsync(reservation.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Reservation!.ActualStartDateTime.Should().Be(Now);
            result.Reservation.Status.Should().Be(ReservationStatus.Booked);
        }
    }

    [Fact]
    public async Task StartAsync_WhenMeetingIsInTheFuture_ReturnsFailure()
    {
        // Arrange
        var (context, service) = CreateSut();

        var reservation = new Reservation
        {
            RoomId = 1,
            OrganizerId = 1,
            Title = "Future meeting",
            StartDateTime = Now.AddMinutes(15),
            EndDateTime = Now.AddHours(1),
            AttendeesCount = 3,
            Status = ReservationStatus.Booked
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        // Act
        var result = await service.StartAsync(reservation.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("START_MEETING_NOT_STARTED");
        (await context.Reservations.FindAsync(reservation.Id))!
            .ActualStartDateTime.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_WhenMeetingHasAlreadyStarted_ReturnsFailure()
    {
        // Arrange
        var (context, service) = CreateSut();

        var reservation = new Reservation
        {
            RoomId = 1,
            OrganizerId = 1,
            Title = "Already started meeting",
            StartDateTime = Now.AddMinutes(-10),
            EndDateTime = Now.AddHours(1),
            ActualStartDateTime = Now.AddMinutes(-5),
            AttendeesCount = 3,
            Status = ReservationStatus.Booked
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        // Act
        var result = await service.StartAsync(reservation.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("MEETING_ALREADY_STARTED");
    }

    [Fact]
    public async Task StartAsync_WhenScheduledEndTimeHasPassed_ReturnsFailure()
    {
        // Arrange
        var (context, service) = CreateSut();

        var reservation = new Reservation
        {
            RoomId = 1,
            OrganizerId = 1,
            Title = "Expired meeting",
            StartDateTime = Now.AddHours(-2),
            EndDateTime = Now,
            AttendeesCount = 3,
            Status = ReservationStatus.Booked
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        // Act
        var result = await service.StartAsync(reservation.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("START_MEETING_TIME_EXPIRED");
    }

    [Fact]
    public async Task StartAsync_UnknownReservation_ReturnsNotFound()
    {
        // Arrange
        var (context, service) = CreateSut();
        using (context)
        {
            // Act
            var result = await service.StartAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("RESERVATION_NOT_FOUND");
        }
    }

    [Fact]
    public async Task StartAsync_CancelledReservation_ReturnsNotBooked()
    {
        // Arrange
        var (context, service) = CreateSut();
        using (context)
        {
            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Cancelled meeting",
                StartDateTime = Now,
                EndDateTime = Now.AddHours(1),
                AttendeesCount = 3,
                Status = ReservationStatus.Cancelled
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            // Act
            var result = await service.StartAsync(reservation.Id);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("START_RESERVATION_NOT_BOOKED");
        }
    }
}
