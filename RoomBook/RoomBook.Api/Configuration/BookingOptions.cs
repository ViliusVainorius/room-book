namespace RoomBook.Api.Configuration;

public class BookingOptions
{
    public int MaxDurationDays { get; set; } = 7;

    public int MaxBookingHorizonDays { get; set; } = 90;
}
