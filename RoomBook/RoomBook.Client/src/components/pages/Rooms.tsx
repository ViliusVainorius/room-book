import { useEffect, useState } from "react";
import EquipmentFilter from "../filters/EquipmentFilter";
import RoomsTable, { type Room } from "../tables/RoomsTable";
import { getRooms } from "../api/roomsApi";

function Rooms() {
  const [rooms, setRooms] = useState<Room[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedEquipment, setSelectedEquipment] = useState<string[]>([]);
  const [minCapacity, setMinCapacity] = useState("");
  const [floor, setFloor] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function loadRooms() {
      try {
        setIsLoading(true);
        setError(null);

        const data = await getRooms(controller.signal);
        setRooms(data);
      } catch (err) {
        // Ignore errors caused by cancelling the request.
        if (controller.signal.aborted) {
          return;
        }

        setError(
          err instanceof Error ? err.message : "An unexpected error occurred.",
        );
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    loadRooms();

    return () => controller.abort();
  }, []);

  function handleEquipmentChange(equipment: string) {
    setSelectedEquipment((previous) =>
      previous.includes(equipment)
        ? previous.filter((item) => item !== equipment)
        : [...previous, equipment],
    );
  }

  if (isLoading) {
    return <p>Loading rooms...</p>;
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        Could not load rooms: {error}
        <button
          className="btn btn-outline-danger btn-sm ms-3"
          onClick={() => window.location.reload()}
        >
          Retry
        </button>
      </div>
    );
  }

  const filteredRooms = rooms.filter((room) => {
    const matchesCapacity =
      minCapacity === "" || room.capacity >= Number(minCapacity);

    const matchesFloor = floor === "" || room.floor === floor;

    const matchesEquipment = selectedEquipment.every((equipment) =>
      room.equipment.includes(equipment),
    );

    return matchesCapacity && matchesFloor && matchesEquipment;
  });

  return (
    <>
      <form className="row g-3 mb-4">
        <div className="col-md-4">
          <label htmlFor="minCapacity" className="form-label">
            MINIMUM CAPACITY
          </label>
          <input
            min="0"
            type="number"
            className="form-control"
            id="minCapacity"
            placeholder="Any capacity"
            value={minCapacity}
            onChange={(event) => setMinCapacity(event.target.value)}
          />
        </div>

        <div className="col-md-8">
          <label htmlFor="floor" className="form-label">
            FLOOR
          </label>
          <input
            type="text"
            className="form-control"
            id="floor"
            placeholder="Any floor or location"
            value={floor}
            onChange={(event) => setFloor(event.target.value)}
          />
        </div>

        <EquipmentFilter
          selectedEquipment={selectedEquipment}
          onEquipmentChange={handleEquipmentChange}
        />
      </form>

      <RoomsTable rooms={filteredRooms} />
    </>
  );
}

export default Rooms;
