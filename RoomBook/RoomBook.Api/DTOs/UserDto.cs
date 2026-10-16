namespace RoomBook.Api.DTOs;

public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    int ActiveReservationsCount
);
