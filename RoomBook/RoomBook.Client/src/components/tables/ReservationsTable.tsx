import { useEffect, useState } from "react";
import { Status } from "../constants/ReservationStatus";
import type { Reservation } from "../constants/ReservationType";
import ReservationStatusBadge from "../ReservationStatusBadge";

type ReservationsTableProps = {
  reservations: Reservation[];
  onCancel: (reservation: Reservation) => void;
  onComplete: (reservation: Reservation) => void;
  cancellingReservationId: number | null;
  completingReservationId: number | null;
};

// Keeps the End early visibility correct when a meeting starts while the
// page is already open, without re-rendering on every frame.
const CLOCK_REFRESH_INTERVAL_MS = 30_000;

function useCurrentTime() {
  const [currentTime, setCurrentTime] = useState(() => Date.now());

  useEffect(() => {
    const intervalId = setInterval(
      () => setCurrentTime(Date.now()),
      CLOCK_REFRESH_INTERVAL_MS,
    );

    return () => clearInterval(intervalId);
  }, []);

  return currentTime;
}

function isMeetingUnderway(reservation: Reservation, currentTime: number) {
  // Mirrors the backend rule: a booked meeting may be ended once its
  // scheduled start is strictly in the past.
  return (
    reservation.status === Status.Booked &&
    new Date(reservation.startDateTime).getTime() < currentTime
  );
}

function ReservationsTable({
  reservations,
  onCancel,
  onComplete,
  cancellingReservationId,
  completingReservationId,
}: ReservationsTableProps) {
  const currentTime = useCurrentTime();

  const isActionPending =
    cancellingReservationId !== null || completingReservationId !== null;

  return (
    <table className="table table-hover align-middle">
      <thead className="table-light">
        <tr>
          <th scope="col">Meeting Title</th>
          <th scope="col">Room</th>
          <th scope="col">User</th>
          <th scope="col">Date</th>
          <th scope="col">Number of attendees</th>
          <th scope="col">Status</th>
          <th scope="col" className="text-end">
            Actions
          </th>
        </tr>
      </thead>

      <tbody>
        {reservations.map((reservation) => (
          <tr key={reservation.id}>
            <td>{reservation.title}</td>
            <td>{reservation.roomName}</td>
            <td>
              {reservation.organizerFirstName} {reservation.organizerLastName}
            </td>
            <td>
              <div>
                {new Date(reservation.startDateTime).toLocaleDateString()}
              </div>
              <small className="text-secondary">
                {new Date(reservation.startDateTime).toLocaleTimeString([], {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
                {" - "}
                {new Date(reservation.endDateTime).toLocaleTimeString([], {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
              </small>
            </td>
            <td>{reservation.attendeesCount}</td>
            <td>
              <ReservationStatusBadge status={reservation.status} />
            </td>
            <td className="text-end">
              <div className="d-flex gap-2 justify-content-end">
                {isMeetingUnderway(reservation, currentTime) && (
                  <button
                    type="button"
                    className="btn btn-success btn-sm"
                    aria-label={`End meeting early ${reservation.title}`}
                    onClick={() => onComplete(reservation)}
                    disabled={isActionPending}
                  >
                    {completingReservationId === reservation.id
                      ? "Ending..."
                      : "End early"}
                  </button>
                )}

                <button
                  type="button"
                  className="btn btn-danger btn-sm"
                  aria-label={`Cancel reservation ${reservation.title}`}
                  onClick={() => onCancel(reservation)}
                  disabled={
                    reservation.status !== Status.Booked || isActionPending
                  }
                >
                  {cancellingReservationId === reservation.id
                    ? "Cancelling..."
                    : "Cancel"}
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export default ReservationsTable;
