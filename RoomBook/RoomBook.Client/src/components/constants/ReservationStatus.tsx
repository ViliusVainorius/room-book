export const Status = {
  Booked: "Booked",
  Completed: "Completed",
  Cancelled: "Cancelled",
} as const;

export type ReservationStatus = (typeof Status)[keyof typeof Status];
