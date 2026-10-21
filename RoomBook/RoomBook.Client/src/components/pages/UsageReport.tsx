import { useEffect, useState } from "react";
import {
  getUtilization,
  type RoomUtilization,
} from "../api/roomsApi";

// "Booked but rarely used": a room where half or less of the scheduled
// time was actually used is flagged for review.
const LOW_USAGE_PERCENT = 50;

function formatHours(hours: number): string {
  const rounded = Math.round(hours * 10) / 10;
  return `${rounded} h`;
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="col-6 col-md-3">
      <div className="card h-100">
        <div className="card-body">
          <div className="text-secondary small">{label}</div>
          <div className="fs-4 fw-semibold">{value}</div>
        </div>
      </div>
    </div>
  );
}

function UsageReport() {
  const [report, setReport] = useState<RoomUtilization[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    async function loadReport() {
      try {
        setIsLoading(true);
        setError(null);

        const data = await getUtilization(controller.signal);
        setReport(data);
      } catch (err) {
        // Ignore errors caused by cancelling the request.
        if (controller.signal.aborted) {
          return;
        }

        setError(
          err instanceof Error ? err.message : "An unexpected error occurred.",
        );
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void loadReport();

    return () => controller.abort();
  }, [reloadToken]);

  if (isLoading) {
    return <p>Loading usage report...</p>;
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        Could not load the usage report: {error}
        <button
          type="button"
          className="btn btn-outline-danger btn-sm ms-3"
          onClick={() => setReloadToken((token) => token + 1)}
        >
          Retry
        </button>
      </div>
    );
  }

  // The endpoint has already calculated hours and percentages per room;
  // these are only display totals for the summary cards.
  const totalCompleted = report.reduce(
    (sum, room) => sum + room.completedReservationsCount,
    0,
  );
  const totalBookedHours = report.reduce(
    (sum, room) => sum + room.scheduledHours,
    0,
  );
  const totalActualHours = report.reduce(
    (sum, room) => sum + room.actualHours,
    0,
  );

  // Both bars are scaled against the same maximum, so bar lengths are
  // comparable between rooms.
  const maxBookedHours = Math.max(
    0,
    ...report.map((room) => room.scheduledHours),
  );
  const widthOf = (hours: number) =>
    maxBookedHours > 0
      ? Math.min((hours / maxBookedHours) * 100, 100)
      : 0;

  const roomsToReview = report.filter(
    (room) =>
      room.usagePercentage !== null &&
      room.usagePercentage <= LOW_USAGE_PERCENT,
  );

  return (
    <>
      <p className="text-secondary mb-4">
        Understand how meeting rooms are being used.
      </p>

      <div className="row g-3 mb-4">
        <StatCard label="Rooms reported" value={String(report.length)} />
        <StatCard label="Completed meetings" value={String(totalCompleted)} />
        <StatCard label="Booked hours" value={formatHours(totalBookedHours)} />
        <StatCard label="Actual usage" value={formatHours(totalActualHours)} />
      </div>

      <h5>Booked vs. actual hours</h5>
      <div className="d-flex flex-column gap-4 mb-4 mt-3">
        {report.map((room) => (
          <div key={room.roomId}>
            <div className="d-flex justify-content-between align-items-baseline">
              <span className="fw-semibold">
                {room.roomName}
                {!room.isActive && (
                  <span className="badge text-bg-secondary ms-2">
                    Inactive
                  </span>
                )}
              </span>
              <span className="text-secondary small">
                {formatHours(room.scheduledHours)} booked ·{" "}
                {formatHours(room.actualHours)} used
              </span>
            </div>

            <div
              className="progress mt-2"
              style={{ height: "0.5rem" }}
              aria-hidden="true"
            >
              <div
                className="progress-bar bg-secondary"
                style={{ width: `${widthOf(room.scheduledHours)}%` }}
              />
            </div>
            <div
              className="progress mt-1"
              style={{ height: "0.5rem" }}
              aria-hidden="true"
            >
              <div
                className="progress-bar bg-success"
                style={{ width: `${widthOf(room.actualHours)}%` }}
              />
            </div>

            <div className="small text-secondary mt-1">
              {room.usagePercentage !== null
                ? `Actual usage: ${Math.round(room.usagePercentage)}% of booked hours`
                : "No completed meetings in this period."}
            </div>
          </div>
        ))}
      </div>

      <h5>Rooms to review</h5>
      <div className="d-flex flex-column gap-2 mt-3">
        {roomsToReview.length === 0 ? (
          <p className="text-secondary mb-0">
            No rooms to review — booked time is being used well.
          </p>
        ) : (
          roomsToReview.map((room) => (
            <div key={room.roomId} className="card">
              <div className="card-body py-2 d-flex justify-content-between align-items-center">
                <div>
                  <div className="fw-semibold">{room.roomName}</div>
                  <div className="small text-secondary mb-0">
                    Frequently booked but comparatively little used ·{" "}
                    {formatHours(room.scheduledHours)} booked ·{" "}
                    {formatHours(room.actualHours)} used.
                  </div>
                </div>
                <span className="badge text-bg-danger">
                  Low actual usage
                </span>
              </div>
            </div>
          ))
        )}
      </div>
    </>
  );
}

export default UsageReport;
