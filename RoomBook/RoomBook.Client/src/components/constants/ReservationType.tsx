import type { ReservationStatus } from "./ReservationStatus";

export type Reservation = {
  id: number;
  title: string;
  organizerFirstName: string;
  organizerLastName: string;
  roomId: number;
  roomName: string;
  organizerId: number;
  startDateTime: string;
  endDateTime: string;
  attendeesCount: number;
  status: ReservationStatus;
};
