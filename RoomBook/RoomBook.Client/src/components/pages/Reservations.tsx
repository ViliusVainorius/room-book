import { useState } from "react";
import {
  cancelReservation,
  completeReservation,
  type ReservationFilters,
} from "../api/reservationsApi";
import ReservationsTable, {
  type CompletionMode,
} from "../tables/ReservationsTable";
import type { ReservationStatus } from "../constants/ReservationStatus";
import type { Reservation } from "../constants/ReservationType";
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
  const [actionError, setActionError] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<{
    id: number;
    kind: "cancel" | "complete";
  } | null>(null);

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

  async function runReservationAction(
    reservation: Reservation,
    kind: "cancel" | "complete",
    request: () => Promise<unknown>,
    successMessage: string,
    fallbackErrorMessage: string,
  ) {
    setPendingAction({ id: reservation.id, kind });
    setActionError(null);
    setSuccessMessage(null);

    try {
      await request();

      setSuccessMessage(successMessage);

      // Keep the active filters and the unfiltered room/organizer options.
      await refreshReservations(lastRequestedFilters);
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : fallbackErrorMessage,
      );
    } finally {
      setPendingAction(null);
    }
  }

  function handleCancel(reservation: Reservation) {
    void runReservationAction(
      reservation,
      "cancel",
      () => cancelReservation(reservation.id),
      `Reservation "${reservation.title}" was cancelled.`,
      "Failed to cancel the reservation. Please try again.",
    );
  }

  function handleComplete(
    reservation: Reservation,
    mode: CompletionMode,
  ) {
    const successMessage =
      mode === "end-early"
        ? `Meeting "${reservation.title}" was ended early.`
        : `Meeting "${reservation.title}" was completed.`;

    void runReservationAction(
      reservation,
      "complete",
      () => completeReservation(reservation.id),
      successMessage,
      "Failed to complete the meeting. Please try again.",
    );
  }

  return (
    <>
      {successMessage && (
        <div className="alert alert-success" role="alert">
          {successMessage}
        </div>
      )}
      {actionError && (
        <div className="alert alert-danger" role="alert">
          {actionError}
        </div>
      )}
      <button
        type="button"
        className="btn btn-primary mb-3"
        onClick={() => {
          setSuccessMessage(null);
          setActionError(null);
          setIsCreateModalOpen(true);
        }}
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
        <ReservationsTable
          reservations={reservations}
          onCancel={handleCancel}
          onComplete={handleComplete}
          cancellingReservationId={
            pendingAction?.kind === "cancel" ? pendingAction.id : null
          }
          completingReservationId={
            pendingAction?.kind === "complete" ? pendingAction.id : null
          }
        />
      )}
    </>
  );
}

export default Reservations;
