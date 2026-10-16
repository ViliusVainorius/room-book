using RoomBook.Domain.Models;

namespace RoomBook.Api.DTOs;

public record ReservationResult(
    Reservation? Reservation,
    string? ErrorCode,
    string? ErrorMessage)
{
    public bool IsSuccess => Reservation is not null;

    public static ReservationResult Success(Reservation reservation) =>
        new(reservation, null, null);

    public static ReservationResult Failure(
        string code,
        string message) =>
        new(null, code, message);
}
