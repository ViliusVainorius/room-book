# RoomBook — meeting-room booking

Web application for booking office meeting rooms. This repository currently
contains the **ASP.NET Core back end** (EF Core in-memory database); the React
front end is planned.

Users can browse rooms, book them, and manage reservations. The system also
reports how rooms are actually used, so the office can see which rooms are
often booked but rarely used.

## Run

```bash
dotnet run --project RoomBook/RoomBook.Api
```

Swagger UI is available in Development at `/swagger`.

## API

| Method | Route | Notes |
|---|---|---|
| GET | `/api/rooms` | Filters: `minCapacity`, `floor`, `equipment` (repeat the parameter for multiple items — a room must have **all** of them) |
| GET | `/api/rooms/utilization` | Usage report; optional `from`/`to` (defaults to the last 30 days) |
| GET | `/api/users` | Includes each user's active (booked) reservation count |
| GET | `/api/reservations` | Filters: `roomId`, `organizerId`, `status`, `startDate`, `endDate` (overlap semantics) |
| POST | `/api/reservations` | Validated against booking rules R1–R7 |
| PATCH | `/api/reservations/{id}/start` | Records the actual meeting start |
| PATCH | `/api/reservations/{id}/complete` | Ends a meeting early (implicit start once the scheduled start has passed) |
| PATCH | `/api/reservations/{id}/cancel` | Cancelled reservations stop blocking the room |

All errors — booking-rule violations and model validation alike — use one
contract:

```json
{ "code": "ROOM_BOOKING_CONFLICT", "message": "The room already has a reservation during this time." }
```

## Configuration

Booking limits live in `appsettings.json` (never hard-coded):

```json
"Booking": {
  "MaxDurationDays": 7,
  "MaxBookingHorizonDays": 90
}
```

## Tests

xUnit + FluentAssertions, organized per layer (`Services/`, `Controllers/`,
`Testing/`):

```bash
dotnet run --project RoomBook/RoomBook.Tests
```

## Architecture

- `RoomBook.Domain` — entities and enums, no dependencies
- `RoomBook.Infrastructure` — `DbContext`, mappings, seed data
- `RoomBook.Api` — controllers, DTOs, booking-rule services, configuration
- `RoomBook.Tests` — unit tests for the services and controllers
