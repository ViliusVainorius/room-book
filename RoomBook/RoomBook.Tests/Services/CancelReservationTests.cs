using FluentAssertions;
using RoomBook.Domain.Models;
using RoomBook.Unit.Tests.Testing;
using Xunit;
using static RoomBook.Unit.Tests.Testing.ReservationTestData;

namespace RoomBook.Unit.Tests.Services;

public class CancelReservationTests
{
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
    public async Task CancelAsync_UnknownReservation_ReturnsNotFound()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var result = await service.CancelAsync(999);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("RESERVATION_NOT_FOUND");
        }
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelledReservation_ReturnsNotBooked()
    {
        var (context, service) = CreateSut();
        using (context)
        {
            var reservation = new Reservation
            {
                RoomId = 1,
                OrganizerId = 1,
                Title = "Already Cancelled Meeting",
                StartDateTime = ValidRequest().StartDateTime,
                EndDateTime = ValidRequest().EndDateTime,
                AttendeesCount = 3,
                Status = ReservationStatus.Cancelled
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var result = await service.CancelAsync(reservation.Id);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should()
                .Be("CANCEL_RESERVATION_NOT_BOOKED");
        }
    }
}
