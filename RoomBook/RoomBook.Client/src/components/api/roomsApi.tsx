import type { Room } from "../tables/RoomsTable";

export type RoomUtilization = {
  roomId: number;
  roomName: string;
  floor: string;
  isActive: boolean;
  reservationCount: number;
  completedReservationsCount: number;
  cancelledReservationsCount: number;
  scheduledHours: number;
  actualHours: number;
  usagePercentage: number | null;
};

export async function getRooms(signal?: AbortSignal): Promise<Room[]> {
  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

  const response = await fetch(`${API_BASE_URL}/api/rooms`, {
    signal,
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch rooms. HTTP status: ${response.status}`);
  }

  return (await response.json()) as Room[];
}

// Called without a date range: the backend then reports its default window
// (the last 30 days), which covers all seeded data. A from/to filter can be
// added to the page later without changing this function's shape.
export async function getUtilization(
  signal?: AbortSignal,
): Promise<RoomUtilization[]> {
  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

  const response = await fetch(`${API_BASE_URL}/api/rooms/utilization`, {
    signal,
  });

  if (!response.ok) {
    throw new Error(
      `Failed to fetch the usage report. HTTP status: ${response.status}`,
    );
  }

  return (await response.json()) as RoomUtilization[];
}

// The PDF is rendered by the backend from the same utilization data, so
// the browser only downloads and saves the returned file.
export async function downloadUtilizationReportPdf(): Promise<void> {
  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

  const response = await fetch(`${API_BASE_URL}/api/rooms/utilization/export`);

  if (!response.ok) {
    throw new Error(
      `Failed to export the usage report. HTTP status: ${response.status}`,
    );
  }

  const blobUrl = URL.createObjectURL(await response.blob());
  const link = document.createElement("a");
  link.href = blobUrl;
  link.download = "room-usage-report.pdf";
  link.click();
  URL.revokeObjectURL(blobUrl);
}
