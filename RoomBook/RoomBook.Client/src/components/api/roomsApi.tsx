import type { Room } from "../tables/RoomsTable";

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
