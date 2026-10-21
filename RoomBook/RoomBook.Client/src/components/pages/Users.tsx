import { useEffect, useState } from "react";
import type { User } from "../tables/UsersTable";
import UsersTable from "../tables/UsersTable";
import { getUsers } from "../api/usersApi";

function Users() {
  const [users, setUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadUsers() {
      try {
        setIsLoading(true);
        setError(null);

        const data = await getUsers(controller.signal);
        setUsers(data);
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

    void loadUsers();

    return () => controller.abort();
  }, []);

  if (isLoading) {
    return <p>Loading users...</p>;
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        Could not load users, error message: {error}
        <button
          type="button"
          className="btn btn-outline-danger btn-sm ms-3"
          onClick={() => window.location.reload()}
        >
          Retry
        </button>
      </div>
    );
  }

  return <UsersTable users={users} />;
}

export default Users;
