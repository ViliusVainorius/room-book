import { useState } from "react";
import { type ReservationFilters } from "../api/reservationsApi";
import ReservationsTable from "../tables/ReservationsTable";
import type { ReservationStatus } from "../constants/ReservationStatus";
import ReservationFiltersPanel from "../filters/ReservationFiltersPanel";
import { useReservations } from "../hooks/useReservations";
import CreateReservation from "../CreateReservation";

function Reservations() {
  const {
    reservations,
    allReservations,
    isLoading,
    error,
    lastRequestedFilters,
    refreshReservations,
  } = useReservations();

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedRoom, setSelectedRoom] = useState("");
  const [selectedOrganizer, setSelectedOrganizer] = useState("");
  const [selectedStatus, setSelectedStatus] = useState<ReservationStatus | "">(
    "",
  );
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const rooms = [
    ...new Map(allReservations.map((r) => [r.roomId, r.roomName])).entries(),
  ];

  const organizers = [
    ...new Map(
      allReservations.map((r) => [
        r.organizerId,
        `${r.organizerFirstName} ${r.organizerLastName}`,
      ]),
    ).entries(),
  ];

  function applyFilters() {
    const filters: ReservationFilters = {
      roomId: selectedRoom ? Number(selectedRoom) : undefined,
      organizerId: selectedOrganizer ? Number(selectedOrganizer) : undefined,
      status: selectedStatus || undefined,
      startDate: startDate || undefined,
      endDate: endDate || undefined,
    };

    void refreshReservations(filters);
  }

  function clearFilters() {
    setSelectedRoom("");
    setSelectedOrganizer("");
    setSelectedStatus("");
    setStartDate("");
    setEndDate("");

    void refreshReservations({}, true);
  }

  return (
    <>
      <h2 className="mb-4">Reservations</h2>

      {successMessage && (
        <div className="alert alert-success" role="alert">
          {successMessage}
        </div>
      )}
      <button
        type="button"
        className="btn btn-primary mb-3"
        onClick={() => setIsCreateModalOpen(true)}
      >
        New reservation
      </button>
      <CreateReservation
        show={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onCreated={() => {
          setSuccessMessage("Reservation created successfully!");
          void refreshReservations({}, true);
        }}
      />

      <ReservationFiltersPanel
        rooms={rooms}
        organizers={organizers}
        selectedRoom={selectedRoom}
        onRoomChange={setSelectedRoom}
        selectedOrganizer={selectedOrganizer}
        onOrganizerChange={setSelectedOrganizer}
        selectedStatus={selectedStatus}
        onStatusChange={setSelectedStatus}
        startDate={startDate}
        onStartDateChange={setStartDate}
        endDate={endDate}
        onEndDateChange={setEndDate}
        onApply={applyFilters}
        onClear={clearFilters}
      />

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
          <button
            type="button"
            className="btn btn-outline-danger btn-sm ms-3"
            onClick={() => void refreshReservations(lastRequestedFilters)}
          >
            Retry
          </button>
        </div>
      )}

      {isLoading ? (
        <p>Loading reservations...</p>
      ) : error ? null : reservations.length === 0 ? (
        <p className="text-secondary">No reservations match your filters.</p>
      ) : (
        <ReservationsTable reservations={reservations} />
      )}
    </>
  );
}

export default Reservations;
