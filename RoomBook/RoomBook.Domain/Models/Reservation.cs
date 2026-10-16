namespace RoomBook.Domain.Models;

public class Reservation
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public DateTime? ActualStartDateTime { get; set; }
    public DateTime? ActualEndDateTime { get; set; }
    public int AttendeesCount { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Booked;
}