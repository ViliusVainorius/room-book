using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomBook.Api.Configuration;
using RoomBook.Api.DTOs;
using RoomBook.Api.Services;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;
using Xunit;

namespace RoomBook.Unit.Tests;

public class ReservationServiceTests
{
    private static readonly DateTime Now =
        new(2026, 10, 16, 9, 0, 0, DateTimeKind.Utc);

    private static (RoomBookDbContext Context, ReservationService Service)
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

    private static CreateReservationRequest ValidRequest() =>
        new(
            RoomId: 1,
            OrganizerId: 1,
            Title: "Team Meeting",
            StartDateTime: Now.AddDays(1),
            EndDateTime: Now.AddDays(1).AddHours(1),
            AttendeesCount: 3);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesReservation()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var result = await service.CreateAsync(ValidRequest());

            result.IsSuccess.Should().BeTrue();
            result.Reservation!.Status
                .Should().Be(ReservationStatus.Booked);

            var saved = await context.Reservations.ToListAsync();
            saved.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task CreateAsync_EndBeforeStart_ReturnsInvalidTimeRange()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                EndDateTime = Now.AddDays(1)
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("INVALID_TIME_RANGE");
        }
    }

    [Fact]
    public async Task CreateAsync_StartInPast_ReturnsStartNotInFuture()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                StartDateTime = Now.AddHours(-1),
                EndDateTime = Now.AddHours(1)
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("START_NOT_IN_FUTURE");
        }
    }

    [Fact]
    public async Task CreateAsync_DurationExactlySevenDays_Succeeds()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                StartDateTime = Now.AddDays(1),
                EndDateTime = Now.AddDays(8)
            };

            var result = await service.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateAsync_DurationOverSevenDays_Fails()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                StartDateTime = Now.AddDays(1),
                EndDateTime = Now.AddDays(8).AddSeconds(1)
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("MAX_DURATION_EXCEEDED");
        }
    }

    [Fact]
    public async Task CreateAsync_StartBeyondBookingHorizon_Fails()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                StartDateTime = Now.AddDays(91),
                EndDateTime = Now.AddDays(91).AddHours(1)
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("BOOKING_HORIZON_EXCEEDED");
        }
    }

    [Fact]
    public async Task CreateAsync_AttendeesExceedCapacity_Fails()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                AttendeesCount = 5
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("ROOM_CAPACITY_EXCEEDED");
        }
    }

    [Fact]
    public async Task CreateAsync_InactiveRoom_Fails()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                RoomId = 3
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("ROOM_INACTIVE");
        }
    }

    [Fact]
    public async Task CreateAsync_OverlappingRoomBooking_Fails()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest();

            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 2,
                Title = "Existing Meeting",
                StartDateTime = request.StartDateTime.AddMinutes(15),
                EndDateTime = request.EndDateTime.AddMinutes(15),
                AttendeesCount = 2,
                Status = ReservationStatus.Booked
            });

            await context.SaveChangesAsync();

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("ROOM_BOOKING_CONFLICT");
        }
    }

    [Fact]
    public async Task CreateAsync_OrganizerAlreadyBooked_Fails()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest();

            context.Reservations.Add(new Reservation
            {
                RoomId = 2,
                OrganizerId = 1,
                Title = "Existing Meeting",
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                AttendeesCount = 5,
                Status = ReservationStatus.Booked
            });

            await context.SaveChangesAsync();

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("ORGANIZER_BOOKING_CONFLICT");
        }
    }

    [Fact]
    public async Task CancelAsync_BookedReservation_CancelsReservation()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest();

            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Meeting to Cancel",
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                AttendeesCount = 3,
                Status = ReservationStatus.Booked
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var result = await service.CancelAsync(reservation.Id);

            result.IsSuccess.Should().BeTrue();
            result.Reservation!.Status.Should()
                .Be(ReservationStatus.Cancelled);
        }
    }

    [Fact]
    public async Task CreateAsync_CancelledReservationDoesNotBlockRoom_Succeeds()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest();

            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 2,
                Title = "Cancelled Meeting",
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                AttendeesCount = 2,
                Status = ReservationStatus.Cancelled
            });

            await context.SaveChangesAsync();

            var result = await service.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
        }
    }

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

    private sealed class FixedTimeProvider(DateTime utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new DateTimeOffset(utcNow);
    }
}
