namespace RoomBook.Api.DTOs;

public record ApiErrorResponse(
    string Code,
    string Message
);
