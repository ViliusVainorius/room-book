import { useEffect, useState } from "react";
// import heroImg from "./assets/hero.png";
// import reactLogo from "./assets/react.svg";
// import viteLogo from "./assets/vite.svg";
import "./App.css";
import Header from "./components/Header";
import Sidebar from "./components/Sidebar";

type Reservation = {
  id: string;
  roomName: string;
};

function App() {
  const [data, setData] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDataForPosts = async () => {
      try {
        const response = await fetch(`https://localhost:7101/api/Reservations`);
        if (!response.ok) {
          throw new Error(`HTTP error: Status ${response.status}`);
        }
        const postsData = await response.json();
        setData(postsData);
        console.log(postsData);
        setError(null);
      } catch (err) {
        setError(err.message);
        setData([]);
      } finally {
        setLoading(false);
      }
    };

    fetchDataForPosts();
  }, []);

  return (
    <>
      <Header />
      <Sidebar />
      <h1>Reservations</h1>

      {/* <div>{error}</div> */}
      {data.length === 0 ? (
        <p>No reservations found. - {error}</p>
      ) : (
        <ul>
          {data.map((reservation) => (
            <li key={reservation.id}>
              ID: {reservation.id} — Room: {reservation.roomName}
            </li>
          ))}
        </ul>
      )}
    </>
  );
}

export default App;
