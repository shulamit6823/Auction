import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

// Hides pages from users who may not open them. This is UI only -
// the real protection is [Authorize] on the server.
export default function ProtectedRoute({ adminOnly = false }) {
  const { user, isAdmin } = useAuth();
  const location = useLocation();

  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (adminOnly && !isAdmin) return <Navigate to="/" replace />;

  return <Outlet />;
}
