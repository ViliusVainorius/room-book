# RoomBook - Meeting Room Booking Application

## Overview

RoomBook is a meeting-room booking application built with ASP.NET Core,
Entity Framework Core InMemory, React, TypeScript, and Bootstrap.

The application allows users to view meeting rooms, manage reservations, and
analyze how meeting rooms are actually used.

The repository contains the whole stack: the ASP.NET Core API in
`RoomBook/RoomBook.Api` and the React client in `RoomBook/RoomBook.Client`.

## Technologies

- **Backend:** ASP.NET Core, Entity Framework Core InMemory
- **Frontend:** React, TypeScript, Bootstrap
- **Testing:** xUnit
- **API documentation:** Swagger / OpenAPI

## Features

- **Rooms** - view meeting rooms and filter them by capacity, floor and
  equipment.
- **Users** - view users and their active reservation counts.
- **Reservations** - view and filter reservations, create bookings, cancel
  reservations, and complete meetings or end them early.
- **Booking validation** - server-side validation.
- **Room utilization report** - compare scheduled meeting time with actual
  meeting duration and identify rooms that are frequently booked but
  relatively little used.
- **PDF export** - export the room utilization report as a PDF document.
- **In-memory database** - seed initial data automatically when the
  application starts.

## How to Run the Application

### Prerequisites

- .NET SDK matching the backend target framework (`net10.0`)
- Node.js and npm compatible with the frontend tooling
- Visual Studio (optional)

### Backend

From the repository root, run:

```bash
dotnet run --project RoomBook/RoomBook.Api
```

The default launch profile listens on `http://localhost:5271`; pass
`--launch-profile https` to use `https://localhost:7101` instead.

Alternatively, open `RoomBook/RoomBook.slnx` in Visual Studio and run the API
project using HTTPS.

In the Development environment Swagger is enabled - open `/swagger` on the
API URL to explore and test the available endpoints.

### Frontend

Open a separate terminal and run:

```bash
cd RoomBook/RoomBook.Client
npm install
npm run dev
```

Open the local URL printed by Vite in the terminal.

The client has no fallback API address - it reads `VITE_API_BASE_URL` from
the environment, so point it at the API you started. Create
`RoomBook/RoomBook.Client/.env.local`:

```bash
VITE_API_BASE_URL=https://localhost:7101
```

Restart `npm run dev` after changing it, and use the URL of whichever API
profile you launched.

### Running Tests

From the repository root, run:

```bash
dotnet test RoomBook/RoomBook.slnx
```

You can also run the tests using Visual Studio's Test Explorer.

## Configuration

Booking limits live in `RoomBook/RoomBook.Api/appsettings.json` (never
hard-coded):

```json
"Booking": {
  "MaxDurationDays": 7,
  "MaxBookingHorizonDays": 90
}
```

The utilization report window is configured the same way: when a request
passes no `from`/`to`, the report covers the last `DefaultWindowDays` days
(optional section, default `30`):

```json
"UtilizationReport": {
  "DefaultWindowDays": 30
}
```

## Architecture

- `RoomBook.Domain` - entities and enums, no dependencies
- `RoomBook.Infrastructure` - `DbContext`, mappings, seed data
- `RoomBook.Api` - controllers, DTOs, booking-rule services, configuration
- `RoomBook.Tests` - unit tests for the services and controllers

## API

| Method | Route | Notes |
|---|---|---|
| GET | `/api/rooms` | Filters: `minCapacity`, `floor`, `equipment` (repeat the parameter for multiple items - a room must have **all** of them) |
| GET | `/api/rooms/utilization` | Usage report; optional `from`/`to` (defaults to the last 30 days) |
| GET | `/api/rooms/utilization/export` | Same window as above; returns the report as a downloadable PDF |
| GET | `/api/users` | Includes each user's active (booked) reservation count |
| GET | `/api/reservations` | Filters: `roomId`, `organizerId`, `status`, `startDate`, `endDate` (overlap semantics) |
| GET | `/api/reservations/{id}` | Single reservation, including room and organizer names |
| POST | `/api/reservations` | Validated against booking rules R1–R7 |
| PATCH | `/api/reservations/{id}/complete` | Ends a meeting: while it is running, records the real finish time; once the scheduled end has passed, records the booked end so usage never exceeds the booked window (implicit start once the scheduled start has passed) |
| PATCH | `/api/reservations/{id}/cancel` | Cancelled reservations stop blocking the room |

All errors - booking-rule violations and model validation alike - use one
contract:

```json
{ "code": "ROOM_BOOKING_CONFLICT", "message": "The room already has a reservation during this time." }
```

## Notes

The application uses an EF Core InMemory database. Data is stored in memory
and is generally reset when the backend application restarts. Seed data is
initialized when the application starts.
