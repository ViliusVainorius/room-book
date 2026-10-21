import { NAV_ITEMS, type PageId } from "./navigation";

type SidebarProps = {
  currentPage: PageId;
  onSelect: (pageId: PageId) => void;
};

function Sidebar({ currentPage, onSelect }: SidebarProps) {
  return (
    <aside className="bg-dark text-white p-3 roombook-sidebar">
      <h4 className="mb-4">RoomBook</h4>

      <nav className="nav flex-column gap-2" aria-label="Main navigation">
        {NAV_ITEMS.map((item) => (
          <button
            key={item.id}
            type="button"
            aria-current={currentPage === item.id ? "page" : undefined}
            className={`nav-link text-start ${
              currentPage === item.id
                ? "active bg-primary text-white"
                : "text-white"
            }`}
            onClick={() => onSelect(item.id)}
          >
            {item.label}
          </button>
        ))}
      </nav>
    </aside>
  );
}

export default Sidebar;
