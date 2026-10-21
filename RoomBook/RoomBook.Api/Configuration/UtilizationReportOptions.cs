namespace RoomBook.Api.Configuration;

public class UtilizationReportOptions
{
    // Reporting window used when a request passes no from/to dates.
    public int DefaultWindowDays { get; set; } = 30;
}
