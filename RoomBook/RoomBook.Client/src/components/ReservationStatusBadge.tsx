import type { ReservationStatus } from "./constants/ReservationStatus";

type Props = {
  status: ReservationStatus;
};

function ReservationStatusBadge({ status }: Props) {
  const badgeClass = {
    Booked: "text-bg-primary",
    Completed: "text-bg-success",
    Cancelled: "text-bg-secondary",
  }[status];

  return <span className={`badge ${badgeClass}`}>{status}</span>;
}

export default ReservationStatusBadge;
