export type Room = {
  id: number;
  name: string;
  capacity: number;
  floor: string;
  equipment: string[];
  isActive: boolean;
};

type RoomsTableProps = {
  rooms: Room[];
};

function RoomsTable({ rooms }: RoomsTableProps) {
  return (
    <table className="table table-hover align-middle">
      <thead className="table-light">
        <tr>
          <th scope="col">ID</th>
          <th scope="col">Room</th>
          <th scope="col">Capacity</th>
          <th scope="col">Floor</th>
          <th scope="col">Equipment</th>
          <th scope="col">Status</th>
        </tr>
      </thead>

      <tbody>
        {rooms.map((room) => (
          <tr key={room.id}>
            <td>{room.id}</td>
            <td>{room.name}</td>
            <td>{room.capacity}</td>
            <td>{room.floor}</td>
            <td>{room.equipment.join(", ")}</td>
            <td>
              <span
                className={`badge ${
                  room.isActive ? "text-bg-success" : "text-bg-secondary"
                }`}
              >
                {room.isActive ? "Active" : "Inactive"}
              </span>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export default RoomsTable;
