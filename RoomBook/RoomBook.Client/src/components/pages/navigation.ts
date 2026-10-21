export const NAV_ITEMS = [
  { id: "reservations", label: "Reservations" },
  { id: "rooms", label: "Rooms" },
  { id: "usage-report", label: "Usage report" },
  { id: "users", label: "Users" },
] as const;

export type PageId = (typeof NAV_ITEMS)[number]["id"];
