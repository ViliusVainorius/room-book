using RoomBook.Domain.Models;

namespace RoomBook.Api.DTOs;

public record ReservationOperationResult(
    Reservation? Reservation,
    string? ErrorCode,
    string? ErrorMessage)
{
    public bool IsSuccess => Reservation is not null;

    public static ReservationOperationResult Success(
        Reservation reservation) =>
        new(reservation, null, null);

    public static ReservationOperationResult Failure(
        string code,
        string message) =>
        new(null, code, message);
}
