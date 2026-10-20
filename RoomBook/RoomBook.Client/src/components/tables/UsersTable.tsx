export type User = {
  id: number;
  firstName: string;
  lastName: string;
  activeReservationsCount: number;
};

type UsersTableProps = {
  users: User[];
};

function UsersTable({ users }: UsersTableProps) {
  return (
    <table className="table table-hover align-middle">
      <thead className="table-light">
        <tr>
          <th scope="col">ID</th>
          <th scope="col">First name</th>
          <th scope="col">Last Name</th>
          <th scope="col">Active reservations</th>
        </tr>
      </thead>

      <tbody>
        {users.map((user) => (
          <tr key={user.id}>
            <td>{user.id}</td>
            <td>{user.firstName}</td>
            <td>{user.lastName}</td>
            <td>{user.activeReservationsCount}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export default UsersTable;
