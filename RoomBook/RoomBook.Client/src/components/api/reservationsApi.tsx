const apiUrl = import.meta.env.VITE_API_BASE_URL;

export type ReservationFilters = {
  roomId?: number;
  organizerId?: number;
  status?: string;
  startDate?: string;
  endDate?: string;
};

export async function getReservations(
  filters: ReservationFilters = {},
  signal?: AbortSignal,
) {
  const params = new URLSearchParams();

  if (filters.roomId !== undefined) {
    params.set("roomId", String(filters.roomId));
  }

  if (filters.organizerId !== undefined) {
    params.set("organizerId", String(filters.organizerId));
  }

  if (filters.status) {
    params.set("status", filters.status);
  }

  if (filters.startDate) {
    params.set("startDate", filters.startDate);
  }

  if (filters.endDate) {
    params.set("endDate", filters.endDate);
  }

  const query = params.toString();
  const url = `${apiUrl}/api/reservations${query ? `?${query}` : ""}`;

  const response = await fetch(url, { signal });

  if (!response.ok) {
    throw new Error(
      `Failed to fetch reservations. HTTP status: ${response.status}`,
    );
  }

  return response.json();
}

export type CreateReservationRequest = {
  title: string;
  roomId: number;
  organizerId: number;
  startDateTime: string;
  endDateTime: string;
  attendeesCount: number;
};

export async function createReservation(
  request: CreateReservationRequest,
): Promise<void> {
  const response = await fetch(`${apiUrl}/api/reservations`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
}

type ApiErrorResponse = {
  code?: string;
  message?: string;
};

async function getErrorMessage(response: Response): Promise<string> {
  const body = await response.text();

  if (body) {
    try {
      const error = JSON.parse(body) as ApiErrorResponse;

      if (error.message) {
        return error.message;
      }
    } catch {
      // The response isn't JSON, so use its plain-text body.
    }

    return body;
  }

  return "Something went wrong. Please try again.";
}
