import type { User } from "../tables/UsersTable";

export async function getUsers(signal?: AbortSignal): Promise<User[]> {
  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

  const response = await fetch(`${API_BASE_URL}/api/users`, {
    signal,
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch rooms. HTTP status: ${response.status}`);
  }

  return (await response.json()) as User[];
}
