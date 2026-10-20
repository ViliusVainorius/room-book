import { useState } from "react";
import "./App.css";
// import Rooms from "./components/pages/Rooms";
// import Users from "./components/pages/Users";
import Reservations from "./components/pages/Reservations";
// import Header from "./components/Header";
// import Sidebar from "./components/Sidebar";

const pages = ["Dashboard", "Reservations", "Rooms", "Users", "Utilization"];

function App() {
  const [currentPage, setCurrentPage] = useState("Dashboard");

  return (
    <div className="d-flex min-vh-100 bg-light">
      {/* Sidebar */}
      <aside className="bg-dark text-white p-3 roombook-sidebar">
        <h4 className="mb-4">RoomBook</h4>

        <nav className="nav flex-column gap-2">
          {pages.map((page) => (
            <button
              key={page}
              className={`nav-link text-start ${
                currentPage === page
                  ? "active bg-primary text-white"
                  : "text-white"
              }`}
              onClick={() => setCurrentPage(page)}
            >
              {page}
            </button>
          ))}
        </nav>
      </aside>

      {/* Main content */}
      <main className="flex-grow-1 p-4">
        <header className="mb-4">
          <h1>{currentPage}</h1>
          <p className="text-secondary">
            Manage your meeting rooms and bookings.
          </p>
        </header>

        <section className="card border-0 shadow-sm">
          <div className="card-body p-4">
            {/* <h5>{currentPage}</h5> */}

            {/* <p className="mb-0">
              {currentPage === "Dashboard"
                ? "Welcome to RoomBook."
                : `${currentPage} page content will go here.`}
            </p> */}

            {/* <Rooms /> */}
            {/* <Users /> */}
            <Reservations />
          </div>
        </section>
      </main>
    </div>
  );
}

export default App;
