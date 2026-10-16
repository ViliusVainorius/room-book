using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoomBook.Domain.Models;
using RoomBook.Unit.Tests.Testing;
using Xunit;
using static RoomBook.Unit.Tests.Testing.ReservationTestData;

namespace RoomBook.Unit.Tests.Services;

public class CreateReservationTests
{
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
    public async Task CreateAsync_StartExactlyAtBookingHorizon_Succeeds()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            // R6 boundary: exactly 90 days ahead is still allowed.
            var request = ValidRequest() with
            {
                StartDateTime = Now.AddDays(90),
                EndDateTime = Now.AddDays(90).AddHours(1)
            };

            var result = await service.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateAsync_AttendeesEqualToCapacity_Succeeds()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            // R5 boundary: a full room is allowed, only exceeding it is not.
            var request = ValidRequest() with
            {
                AttendeesCount = 4
            };

            var result = await service.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateAsync_BackToBackRoomBookings_Succeeds()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest();

            context.Reservations.Add(new Reservation
            {
                RoomId = 1,
                OrganizerId = 2,
                Title = "Ending Exactly When The Next One Starts",
                StartDateTime = request.StartDateTime.AddHours(-1),
                EndDateTime = request.StartDateTime,
                AttendeesCount = 2,
                Status = ReservationStatus.Booked
            });

            await context.SaveChangesAsync();

            var result = await service.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateAsync_BackToBackOrganizerBookings_Succeeds()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest();

            // The organizer's previous meeting ends in another room
            // exactly when the new one starts.
            context.Reservations.Add(new Reservation
            {
                RoomId = 2,
                OrganizerId = 1,
                Title = "Previous Meeting In Another Room",
                StartDateTime = request.StartDateTime.AddHours(-1),
                EndDateTime = request.StartDateTime,
                AttendeesCount = 3,
                Status = ReservationStatus.Booked
            });

            await context.SaveChangesAsync();

            var result = await service.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateAsync_UnknownRoom_ReturnsRoomNotFound()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                RoomId = 999
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("ROOM_NOT_FOUND");
        }
    }

    [Fact]
    public async Task CreateAsync_UnknownOrganizer_ReturnsOrganizerNotFound()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var request = ValidRequest() with
            {
                OrganizerId = 999
            };

            var result = await service.CreateAsync(request);

            result.ErrorCode.Should().Be("ORGANIZER_NOT_FOUND");
        }
    }
}
