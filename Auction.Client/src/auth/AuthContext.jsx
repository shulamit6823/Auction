import { createContext, useContext, useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { api, setSessionExpiredHandler, TOKEN_KEY } from '../api/api';

// Keeps the logged-in user and the token for the whole app.
// Saved in localStorage, so a page refresh does not log the user out (until the token expires).
// This is only for the UI: the server checks the token on every request.
const AuthContext = createContext(null);

function loadSession() {
  const saved = localStorage.getItem(TOKEN_KEY);
  if (!saved) return null;

  const session = JSON.parse(saved);
  if (new Date(session.expiresAt) <= new Date()) {
    localStorage.removeItem(TOKEN_KEY);
    return null;
  }
  return session;
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(loadSession);

  // Login and Register both return { token, expiresAt, user } - after registering the user is logged in.
  function saveSession(response) {
    localStorage.setItem(TOKEN_KEY, JSON.stringify(response));
    setSession(response);
  }

  async function login(username, password) {
    saveSession(await api.login(username, password));
  }

  async function register(username, email, password) {
    saveSession(await api.register(username, email, password));
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY);
    setSession(null);
  }

  // Automatic logout when the session is over:
  // 1. at the token's expiry time (a timer), and
  // 2. when the server answers 401 to a request that had a token (api.js calls the handler).
  // In both cases the user is sent to the login page and comes back to the same page after logging in.
  const navigate = useNavigate();
  const location = useLocation();
  const currentPath = useRef(location.pathname);
  currentPath.current = location.pathname;

  useEffect(() => {
    function expire() {
      if (!localStorage.getItem(TOKEN_KEY)) return; // already logged out
      logout();
      navigate('/login', { state: { expired: true, from: currentPath.current } });
    }

    setSessionExpiredHandler(expire);
    if (!session) return undefined;

    const timer = setTimeout(expire, new Date(session.expiresAt) - new Date());
    return () => clearTimeout(timer);
  }, [session, navigate]);

  const user = session?.user ?? null;
  const value = { user, isAdmin: user?.role === 'Admin', login, register, logout };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
