using FluentAssertions;
using RoomBook.Domain.Models;
using RoomBook.Unit.Tests.Testing;
using Xunit;
using static RoomBook.Unit.Tests.Testing.ReservationTestData;

namespace RoomBook.Unit.Tests.Services;

public class CompleteReservationTests
{
    [Fact]
    public async Task CompleteAsync_StartedMeeting_RecordsActualEndTime()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Early Finish",
                StartDateTime = Now.AddHours(-1),
                EndDateTime = Now.AddHours(1),
                AttendeesCount = 3,
                Status = ReservationStatus.Booked
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var result = await service.CompleteAsync(reservation.Id);

            result.IsSuccess.Should().BeTrue();
            result.Reservation!.Status.Should()
                .Be(ReservationStatus.Completed);

            result.Reservation.ActualEndDateTime.Should()
                .Be(Now);

            result.Reservation.ActualEndDateTime.Should()
                .BeBefore(reservation.EndDateTime);
        }
    }

    [Fact]
    public async Task CompleteAsync_WhenMeetingHasNotStarted_ReturnsFailure()
    {
        // Arrange
        var (context, service) = CreateSut();

        var reservation = new Reservation
        {
            RoomId = 1,
            OrganizerId = 1,
            Title = "Not started meeting",
            StartDateTime = Now,
            EndDateTime = Now.AddHours(1),
            AttendeesCount = 3,
            Status = ReservationStatus.Booked
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CompleteAsync(reservation.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COMPLETE_MEETING_NOT_STARTED");

        var savedReservation =
            (await context.Reservations.FindAsync(reservation.Id))!;

        savedReservation.Status.Should().Be(ReservationStatus.Booked);
        savedReservation.ActualEndDateTime.Should().BeNull();
    }

    [Fact]
    public async Task CompleteAsync_WhenMeetingHasStarted_RecordsActualEndAndCompletesMeeting()
    {
        // Arrange
        var (context, service) = CreateSut();

        var reservation = new Reservation
        {
            RoomId = 1,
            OrganizerId = 1,
            Title = "Started meeting",
            StartDateTime = Now.AddMinutes(-30),
            EndDateTime = Now.AddMinutes(30),
            ActualStartDateTime = Now.AddMinutes(-20),
            AttendeesCount = 3,
            Status = ReservationStatus.Booked
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CompleteAsync(reservation.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Reservation!.Status.Should().Be(ReservationStatus.Completed);
        result.Reservation.ActualEndDateTime.Should().Be(Now);

        var savedReservation =
            (await context.Reservations.FindAsync(reservation.Id))!;

        savedReservation.ActualEndDateTime.Should().Be(Now);
        savedReservation.ActualStartDateTime.Should()
            .Be(Now.AddMinutes(-20));
    }

    [Fact]
    public async Task CompleteAsync_WhenMeetingFinishesEarly_ActualDurationIsShorterThanScheduledDuration()
    {
        // Arrange
        var (context, service) = CreateSut();

        var reservation = new Reservation
        {
            RoomId = 1,
            OrganizerId = 1,
            Title = "Early finish meeting",
            StartDateTime = Now.AddMinutes(-30),
            EndDateTime = Now.AddMinutes(30),
            ActualStartDateTime = Now.AddMinutes(-20),
            AttendeesCount = 3,
            Status = ReservationStatus.Booked
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CompleteAsync(reservation.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var completed = result.Reservation!;

        var scheduledDuration =
            completed.EndDateTime - completed.StartDateTime;

        var actualDuration =
            completed.ActualEndDateTime!.Value -
            completed.ActualStartDateTime!.Value;

        scheduledDuration.Should().Be(TimeSpan.FromHours(1));
        actualDuration.Should().Be(TimeSpan.FromMinutes(20));
        actualDuration.Should().BeLessThan(scheduledDuration);
    }

    [Fact]
    public async Task CompleteAsync_WithoutExplicitStart_RecordsScheduledStartAsActualStart()
    {
        // Arrange
        var (context, service) = CreateSut();
        using (context)
        {
            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Implicitly started meeting",
                StartDateTime = Now.AddHours(-1),
                EndDateTime = Now.AddMinutes(30),
                AttendeesCount = 3,
                Status = ReservationStatus.Booked
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CompleteAsync(reservation.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Reservation!.Status.Should().Be(ReservationStatus.Completed);

            result.Reservation.ActualStartDateTime.Should()
                .Be(reservation.StartDateTime);
            result.Reservation.ActualEndDateTime.Should().Be(Now);
        }
    }

    [Fact]
    public async Task CompleteAsync_AfterScheduledEnd_CompletesAndRecordsOverrun()
    {
        // Arrange
        var (context, service) = CreateSut();
        using (context)
        {
            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Overrunning meeting",
                StartDateTime = Now.AddHours(-2),
                EndDateTime = Now.AddHours(-1),
                AttendeesCount = 3,
                Status = ReservationStatus.Booked
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CompleteAsync(reservation.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Reservation!.Status.Should().Be(ReservationStatus.Completed);

            result.Reservation.ActualEndDateTime.Should().Be(Now);
            result.Reservation.ActualEndDateTime.Should()
                .BeAfter(reservation.EndDateTime);
        }
    }

    [Fact]
    public async Task CompleteAsync_UnknownReservation_ReturnsNotFound()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var result = await service.CompleteAsync(999);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("RESERVATION_NOT_FOUND");
        }
    }

    [Fact]
    public async Task CompleteAsync_CancelledReservation_ReturnsNotBooked()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Cancelled meeting",
                StartDateTime = Now.AddHours(-1),
                EndDateTime = Now.AddHours(1),
                AttendeesCount = 3,
                Status = ReservationStatus.Cancelled
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var result = await service.CompleteAsync(reservation.Id);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should()
                .Be("COMPLETE_RESERVATION_NOT_BOOKED");
        }
    }
}
