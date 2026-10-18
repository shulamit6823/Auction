import { NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import Logo from './Logo';

export default function Navbar() {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/');
  }

  return (
    <header className="navbar">
      <Logo />

      <nav>
        <NavLink to="/" end>מכירות פעילות</NavLink>
        {/* Shown to everyone: a guest who clicks it is sent to the login page first. */}
        <NavLink to="/auctions/new">מכירה חדשה</NavLink>
        {user && (
          <>
            <NavLink to="/my-auctions">המכירות שלי</NavLink>
            <NavLink to="/my-bids">ההצעות שלי</NavLink>
            <NavLink to="/favorites">מועדפים</NavLink>
          </>
        )}
        {isAdmin && (
          <>
            <NavLink to="/admin/auctions">ניהול מכירות</NavLink>
            <NavLink to="/admin/users">משתמשים</NavLink>
          </>
        )}
      </nav>

      <div className="user-box">
        {user ? (
          <>
            <span>שלום, {user.username}</span>
            <button className="btn btn-light" onClick={handleLogout}>התנתקות</button>
          </>
        ) : (
          <>
            <NavLink to="/login" className="btn btn-light">התחברות</NavLink>
            <NavLink to="/register" className="btn btn-primary">הרשמה</NavLink>
          </>
        )}
      </div>
    </header>
  );
}
