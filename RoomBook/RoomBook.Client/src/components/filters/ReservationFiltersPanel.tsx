import { Status } from "../constants/ReservationStatus";
import type { ReservationStatus } from "../constants/ReservationStatus";

type Props = {
  rooms: [number, string][];
  organizers: [number, string][];

  selectedRoom: string;
  onRoomChange: (value: string) => void;

  selectedOrganizer: string;
  onOrganizerChange: (value: string) => void;

  selectedStatus: ReservationStatus | "";
  onStatusChange: (value: ReservationStatus | "") => void;

  startDate: string;
  onStartDateChange: (value: string) => void;

  endDate: string;
  onEndDateChange: (value: string) => void;

  onApply: () => void;
  onClear: () => void;
};

export default function ReservationFiltersPanel({
  rooms,
  organizers,
  selectedRoom,
  onRoomChange,
  selectedOrganizer,
  onOrganizerChange,
  selectedStatus,
  onStatusChange,
  startDate,
  onStartDateChange,
  endDate,
  onEndDateChange,
  onApply,
  onClear,
}: Props) {
  return (
    <div className="card mb-4">
      <div className="card-body">
        <div className="row g-3">
          <div className="col-md-6 col-lg-3">
            <label className="form-label">Room</label>
            <select
              className="form-select"
              value={selectedRoom}
              onChange={(e) => onRoomChange(e.target.value)}
            >
              <option value="">All rooms</option>
              {rooms.map(([id, name]) => (
                <option key={id} value={id}>
                  {name}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-6 col-lg-3">
            <label className="form-label">Organizer</label>
            <select
              className="form-select"
              value={selectedOrganizer}
              onChange={(e) => onOrganizerChange(e.target.value)}
            >
              <option value="">All organizers</option>
              {organizers.map(([id, name]) => (
                <option key={id} value={id}>
                  {name}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-6 col-lg-2">
            <label className="form-label">Status</label>
            <select
              className="form-select"
              value={selectedStatus}
              onChange={(e) =>
                onStatusChange(e.target.value as ReservationStatus | "")
              }
            >
              <option value="">All statuses</option>
              {Object.values(Status).map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-6 col-lg-2">
            <label className="form-label">Start date</label>
            <input
              type="datetime-local"
              className="form-control"
              value={startDate}
              max={endDate || undefined}
              onChange={(e) => onStartDateChange(e.target.value)}
            />
          </div>

          <div className="col-md-6 col-lg-2">
            <label className="form-label">End date</label>
            <input
              type="datetime-local"
              className="form-control"
              value={endDate}
              min={startDate || undefined}
              onChange={(e) => onEndDateChange(e.target.value)}
            />
          </div>
        </div>

        <div className="d-flex justify-content-end gap-2 mt-3">
          <button
            type="button"
            className="btn btn-outline-secondary"
            onClick={onClear}
          >
            Clear
          </button>

          <button type="button" className="btn btn-primary" onClick={onApply}>
            Apply filters
          </button>
        </div>
      </div>
    </div>
  );
}
