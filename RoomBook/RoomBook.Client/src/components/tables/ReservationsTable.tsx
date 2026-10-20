import type { Reservation } from "../constants/ReservationType";
import ReservationStatusBadge from "../ReservationStatusBadge";

type ReservationsTableProps = {
  reservations: Reservation[];
};

function ReservationsTable({ reservations }: ReservationsTableProps) {
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
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export default ReservationsTable;
