// import reactLogo from "../assets/react.svg";
import viteLogo from "../assets/vite.svg";

function Header() {
  return (
    <>
      <div className="header-flex">
        <img src={viteLogo} alt="aplikacijos mini logo tipas" />
        <div>
          <h3>RoomBook</h3>
          <p>Meeting room management</p>
        </div>
      </div>
    </>
  );
}

export default Header;
