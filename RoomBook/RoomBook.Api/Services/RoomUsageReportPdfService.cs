using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RoomBook.Api.DTOs;

namespace RoomBook.Api.Services;

public class RoomUsageReportPdfService
{
    private const double LowUsagePercentThreshold = 50;

    static RoomUsageReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(
        IReadOnlyList<RoomUtilizationDto> rooms,
        DateTime reportStart,
        DateTime reportEnd,
        DateTime generatedAt)
    {
        return Document.Create(container =>
                ComposeReport(container, rooms, reportStart, reportEnd, generatedAt))
            .GeneratePdf();
    }

    private static void ComposeReport(
        IDocumentContainer container,
        IReadOnlyList<RoomUtilizationDto> rooms,
        DateTime reportStart,
        DateTime reportEnd,
        DateTime generatedAt)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header().Column(column =>
            {
                column.Item().AlignCenter()
                    .Text("MEETING ROOM UTILIZATION REPORT")
                    .FontSize(18)
                    .Bold();

                column.Item().AlignCenter()
                    .Text("RoomBook · Office Space Usage Analysis")
                    .FontColor(Colors.Grey.Darken1);

                column.Item().AlignCenter()
                    .Text(
                        $"Reporting period {reportStart:yyyy-MM-dd} to {reportEnd:yyyy-MM-dd}")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                column.Item().PaddingTop(8)
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Medium);
            });

            page.Content().PaddingTop(20).Column(column =>
            {
                var completedMeetings = rooms.Sum(
                    room => room.CompletedReservationsCount);
                var bookedHours = rooms.Sum(room => room.ScheduledHours);
                var actualHours = rooms.Sum(room => room.ActualHours);
                var roomsToReview = GetRoomsToReview(rooms);

                column.Item().Row(row =>
                {
                    ComposeStat(
                        row.RelativeItem(),
                        "Completed meetings",
                        completedMeetings.ToString());

                    ComposeStat(
                        row.RelativeItem(),
                        "Total booked hours",
                        FormatHours(bookedHours));
                });

                column.Item().PaddingTop(16).Row(row =>
                {
                    ComposeStat(
                        row.RelativeItem(),
                        "Actual usage",
                        FormatHours(actualHours));

                    ComposeStat(
                        row.RelativeItem(),
                        "Rooms to review",
                        roomsToReview.Count.ToString());
                });

                column.Item().PaddingTop(24)
                    .Text("Room utilization details")
                    .FontSize(13)
                    .Bold();

                column.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().PaddingBottom(6).Text("Room").Bold();
                        header.Cell().PaddingBottom(6).AlignRight()
                            .Text("Booked").Bold();
                        header.Cell().PaddingBottom(6).AlignRight()
                            .Text("Actual").Bold();
                        header.Cell().PaddingBottom(6).AlignRight()
                            .Text("Usage").Bold();
                    });

                    foreach (var room in rooms)
                    {
                        table.Cell().PaddingVertical(6)
                            .Text(room.RoomName);
                        table.Cell().PaddingVertical(6).AlignRight()
                            .Text(FormatHours(room.ScheduledHours));
                        table.Cell().PaddingVertical(6).AlignRight()
                            .Text(FormatHours(room.ActualHours));
                        table.Cell().PaddingVertical(6).AlignRight()
                            .Text(FormatPercentage(room.UsagePercentage));
                    }
                });

                column.Item().PaddingTop(24)
                    .Text("Observations")
                    .FontSize(13)
                    .Bold();

                column.Item().PaddingTop(6)
                    .Text(ComposeObservation(roomsToReview));
            });

            page.Footer().PaddingTop(16).AlignCenter().Text(text =>
            {
                text.Span($"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC · Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private static void ComposeStat(
        IContainer container,
        string label,
        string value)
    {
        container.Column(column =>
        {
            column.Item().Text(label).FontColor(Colors.Grey.Darken1);
            column.Item().Text(value).FontSize(16).Bold();
        });
    }

    private static List<RoomUtilizationDto> GetRoomsToReview(
        IReadOnlyList<RoomUtilizationDto> rooms) =>
        rooms
            .Where(room =>
                room.UsagePercentage is not null &&
                room.UsagePercentage <= LowUsagePercentThreshold)
            .ToList();

    private static string ComposeObservation(
        IReadOnlyList<RoomUtilizationDto> roomsToReview) =>
        roomsToReview.Count == 0
            ? "All rooms show healthy usage during the reporting period: " +
              "no room used half or less of its booked hours."
            : string.Join(
                " ",
                roomsToReview.Select(room =>
                    $"{room.RoomName} has a comparatively low actual-to-booked " +
                    $"usage ratio ({Math.Round(room.UsagePercentage!.Value)}%). " +
                    "Its booking patterns may warrant further review."));

    private static string FormatHours(double hours) =>
        $"{hours.ToString("0.#", CultureInfo.InvariantCulture)} h";

    private static string FormatPercentage(double? percentage) =>
        percentage.HasValue
            ? $"{percentage.Value.ToString("0", CultureInfo.InvariantCulture)}%"
            : "—";
}
