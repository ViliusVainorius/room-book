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

const apiUrl = import.meta.env.VITE_API_BASE_URL;

export async function getRooms(signal?: AbortSignal): Promise<Room[]> {
  const response = await fetch(`${apiUrl}/api/rooms`, {
    signal,
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch rooms. HTTP status: ${response.status}`);
  }

  return (await response.json()) as Room[];
}

export async function getUtilization(
  signal?: AbortSignal,
): Promise<RoomUtilization[]> {
  const response = await fetch(`${apiUrl}/api/rooms/utilization`, {
    signal,
  });

  if (!response.ok) {
    throw new Error(
      `Failed to fetch the usage report. HTTP status: ${response.status}`,
    );
  }

  return (await response.json()) as RoomUtilization[];
}

export async function downloadUtilizationReportPdf(): Promise<void> {
  const response = await fetch(`${apiUrl}/api/rooms/utilization/export`);

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
