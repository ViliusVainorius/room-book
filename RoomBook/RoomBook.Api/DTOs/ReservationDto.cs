using RoomBook.Domain.Models;

namespace RoomBook.Api.DTOs;

public record ReservationDto(
    int Id,
    int RoomId,
    string RoomName,
    int OrganizerId,
    string OrganizerFirstName,
    string OrganizerLastName,
    string Title,
    DateTime StartDateTime,
    DateTime EndDateTime,
    int AttendeesCount,
    ReservationStatus Status,
    DateTime? ActualEndDateTime,
    DateTime? ActualStartDateTime
);
