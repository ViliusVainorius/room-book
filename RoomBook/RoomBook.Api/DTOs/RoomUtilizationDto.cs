namespace RoomBook.Api.DTOs;

public record RoomUtilizationDto(
    int RoomId,
    string RoomName,
    string Floor,
    bool IsActive,
    int ReservationCount,
    int CompletedReservationsCount,
    int CancelledReservationsCount,
    double ScheduledHours,
    double ActualHours,
    double? UsagePercentage
);
