using RoomBook.Domain.Models;

namespace RoomBook.Api.DTOs;

public record ReservationCreationResult(
    Reservation? Reservation,
    string? ErrorCode,
    string? ErrorMessage)
{
    public bool IsSuccess => Reservation is not null;

    public static ReservationCreationResult Success(
        Reservation reservation) =>
        new(reservation, null, null);

    public static ReservationCreationResult Failure(
        string code,
        string message) =>
        new(null, code, message);
}
