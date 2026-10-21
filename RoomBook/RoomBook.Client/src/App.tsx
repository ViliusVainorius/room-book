import { useState } from "react";
import type { ComponentType } from "react";
import "./App.css";

import Sidebar from "./components/pages/Sidebar";
import Reservations from "./components/pages/Reservations";
import Rooms from "./components/pages/Rooms";
import Users from "./components/pages/Users";
import UsageReport from "./components/pages/UsageReport";
import { NAV_ITEMS, type PageId } from "./components/pages/navigation";

// Every navigation item must have a page component here, so adding a new
// sidebar entry without wiring it up fails at compile time.
const pageComponents: Record<PageId, ComponentType> = {
  reservations: Reservations,
  rooms: Rooms,
  "usage-report": UsageReport,
  users: Users,
};

function App() {
  const [currentPageId, setCurrentPageId] = useState<PageId>("reservations");

  const currentPageLabel =
    NAV_ITEMS.find((item) => item.id === currentPageId)?.label ?? "";
  const CurrentPage = pageComponents[currentPageId];

  return (
    <div className="d-flex min-vh-100 bg-light">
      <Sidebar currentPage={currentPageId} onSelect={setCurrentPageId} />

      <main className="flex-grow-1 p-4">
        <header className="mb-4">
          <h1>{currentPageLabel}</h1>
        </header>

        <section className="card border-0 shadow-sm">
          <div className="card-body p-4">
            <CurrentPage />
          </div>
        </section>
      </main>
    </div>
  );
}

export default App;
