using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.DTOs;

public record CreateReservationRequest(
    [Range(1, int.MaxValue)]
    int RoomId,

    [Range(1, int.MaxValue)]
    int OrganizerId,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Title,

    DateTime StartDateTime,

    DateTime EndDateTime,

    [Range(1, int.MaxValue)]
    int AttendeesCount
);
