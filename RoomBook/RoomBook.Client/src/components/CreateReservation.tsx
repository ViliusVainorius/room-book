import { useEffect, useState } from "react";
import type { FormEvent, ChangeEvent } from "react";

import type { Room } from "./tables/RoomsTable";
import type { User } from "./tables/UsersTable";

import { getRooms } from "./api/roomsApi";
import { getUsers } from "./api/usersApi";
import { createReservation } from "./api/reservationsApi";

type CreateReservationProps = {
  show: boolean;
  onClose: () => void;
  onCreated: () => void;
};

type ReservationFormData = {
  title: string;
  roomId: string;
  organizerId: string;
  startDateTime: string;
  endDateTime: string;
  attendeesCount: string;
};

const initialFormData: ReservationFormData = {
  title: "",
  roomId: "",
  organizerId: "",
  startDateTime: "",
  endDateTime: "",
  attendeesCount: "",
};

function CreateReservation({
  show,
  onClose,
  onCreated,
}: CreateReservationProps) {
  const [rooms, setRooms] = useState<Room[]>([]);
  const [users, setUsers] = useState<User[]>([]);

  const [isLoadingOptions, setIsLoadingOptions] = useState(false);
  const [optionsError, setOptionsError] = useState<string | null>(null);

  const [formData, setFormData] =
    useState<ReservationFormData>(initialFormData);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [apiMessage, setApiMessage] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  function handleChange(
    event: ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) {
    const { name, value } = event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setIsSubmitting(true);
    setApiMessage(null);
    setIsSuccess(false);

    try {
      await createReservation({
        title: formData.title.trim(),
        roomId: Number(formData.roomId),
        organizerId: Number(formData.organizerId),
        startDateTime: formData.startDateTime,
        endDateTime: formData.endDateTime,
        attendeesCount: Number(formData.attendeesCount),
      });

      onCreated();
      setFormData(initialFormData);
      onClose();
    } catch (error) {
      setApiMessage(
        error instanceof Error
          ? error.message
          : "Something went wrong. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  useEffect(() => {
    if (!show) return;

    const controller = new AbortController();

    async function loadOptions() {
      setIsLoadingOptions(true);
      setOptionsError(null);

      try {
        const [roomData, userData] = await Promise.all([
          getRooms(controller.signal),
          getUsers(controller.signal),
        ]);

        if (controller.signal.aborted) return;

        setRooms(roomData.filter((room) => room.isActive));
        setUsers(userData);
      } catch (error) {
        if (controller.signal.aborted) return;

        setOptionsError(
          error instanceof Error
            ? error.message
            : "Failed to load rooms and users.",
        );
      } finally {
        if (!controller.signal.aborted) {
          setIsLoadingOptions(false);
        }
      }
    }

    void loadOptions();

    return () => controller.abort();
  }, [show]);

  if (!show) {
    return null;
  }

  return (
    <div
      className="modal d-block"
      tabIndex={-1}
      role="dialog"
      aria-modal="true"
      aria-labelledby="createReservationTitle"
      style={{ backgroundColor: "rgba(0, 0, 0, 0.69)" }}
    >
      <div className="modal-dialog modal-lg modal-dialog-centered">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title" id="createReservationTitle">
              New reservation
            </h5>

            <button
              type="button"
              className="btn-close"
              aria-label="Close"
              onClick={onClose}
              disabled={isSubmitting}
            />
          </div>

          <form onSubmit={handleSubmit}>
            <div className="modal-body">
              {optionsError && (
                <div className="alert alert-danger" role="alert">
                  {optionsError}
                </div>
              )}

              {apiMessage && (
                <div
                  className={`alert ${
                    isSuccess ? "alert-success" : "alert-danger"
                  }`}
                  role="alert"
                >
                  {apiMessage}
                </div>
              )}

              <div className="mb-3">
                <label htmlFor="title" className="form-label">
                  Meeting title
                </label>
                <input
                  id="title"
                  name="title"
                  type="text"
                  className="form-control"
                  placeholder="e.g. Sprint planning"
                  value={formData.title}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="row">
                <div className="col-md-6 mb-3">
                  <label htmlFor="roomId" className="form-label">
                    Room
                  </label>
                  <select
                    id="roomId"
                    name="roomId"
                    className="form-select"
                    value={formData.roomId}
                    onChange={handleChange}
                    required
                    disabled={
                      isLoadingOptions || !!optionsError || isSubmitting
                    }
                  >
                    <option value="" disabled>
                      {isLoadingOptions ? "Loading rooms..." : "Select a room"}
                    </option>

                    {rooms.map((room) => (
                      <option key={room.id} value={room.id}>
                        {room.name} - Floor {room.floor} - Capacity{" "}
                        {room.capacity}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="col-md-6 mb-3">
                  <label htmlFor="organizerId" className="form-label">
                    Organizer
                  </label>
                  <select
                    id="organizerId"
                    name="organizerId"
                    className="form-select"
                    value={formData.organizerId}
                    onChange={handleChange}
                    required
                    disabled={
                      isLoadingOptions || !!optionsError || isSubmitting
                    }
                  >
                    <option value="" disabled>
                      {isLoadingOptions
                        ? "Loading users..."
                        : "Select an organizer"}
                    </option>

                    {users.map((user) => (
                      <option key={user.id} value={user.id}>
                        {user.firstName} {user.lastName}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="row">
                <div className="col-md-6 mb-3">
                  <label htmlFor="startDateTime" className="form-label">
                    Start date and time
                  </label>
                  <input
                    id="startDateTime"
                    name="startDateTime"
                    type="datetime-local"
                    className="form-control"
                    value={formData.startDateTime}
                    onChange={handleChange}
                    required
                  />
                </div>

                <div className="col-md-6 mb-3">
                  <label htmlFor="endDateTime" className="form-label">
                    End date and time
                  </label>
                  <input
                    id="endDateTime"
                    name="endDateTime"
                    type="datetime-local"
                    className="form-control"
                    value={formData.endDateTime}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>

              <div className="mb-3">
                <label htmlFor="attendeesCount" className="form-label">
                  Number of attendees
                </label>
                <input
                  id="attendeesCount"
                  name="attendeesCount"
                  type="number"
                  className="form-control"
                  min={1}
                  step={1}
                  required
                  value={formData.attendeesCount}
                  onChange={handleChange}
                  placeholder="Enter attendee count"
                />
              </div>
            </div>

            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={onClose}
                disabled={isSubmitting}
              >
                Cancel
              </button>

              <button
                type="submit"
                className="btn btn-primary"
                disabled={isSubmitting || isLoadingOptions}
              >
                {isSubmitting ? "Creating..." : "Create reservation"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export default CreateReservation;
