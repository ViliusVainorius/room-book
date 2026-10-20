import { useCallback, useEffect, useState } from "react";
import {
  getReservations,
  type ReservationFilters,
} from "../api/reservationsApi";
import type { Reservation } from "../constants/ReservationType";

export function useReservations() {
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [allReservations, setAllReservations] = useState<Reservation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [lastRequestedFilters, setLastRequestedFilters] =
    useState<ReservationFilters>({});

  const loadReservations = useCallback(
    async (
      filters: ReservationFilters,
      signal?: AbortSignal,
      updateAll = false,
    ) => {
      try {
        const data = await getReservations(filters, signal);

        if (signal?.aborted) return;

        setReservations(data);

        if (updateAll) {
          setAllReservations(data);
        }

        setError(null);
      } catch (err) {
        if (signal?.aborted) return;

        setError(
          err instanceof Error ? err.message : "An unexpected error occurred.",
        );
      } finally {
        if (!signal?.aborted) {
          setIsLoading(false);
        }
      }
    },
    [],
  );

  useEffect(() => {
    const controller = new AbortController();

    async function fetchInitialReservations() {
      try {
        const data = await getReservations({}, controller.signal);

        if (controller.signal.aborted) return;

        setReservations(data);
        setAllReservations(data);
        setError(null);
      } catch (err) {
        if (controller.signal.aborted) return;

        setError(
          err instanceof Error ? err.message : "An unexpected error occurred.",
        );
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void fetchInitialReservations();

    return () => controller.abort();
  }, []);

  const refreshReservations = useCallback(
    async (filters: ReservationFilters, updateAll = false) => {
      setLastRequestedFilters(filters);
      setIsLoading(true);
      setError(null);

      await loadReservations(filters, undefined, updateAll);
    },
    [loadReservations],
  );

  return {
    reservations,
    allReservations,
    isLoading,
    error,
    lastRequestedFilters,
    refreshReservations,
  };
}
