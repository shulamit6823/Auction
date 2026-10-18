import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ErrorMessage } from '../components/Messages';

export default function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  // Back to the page that sent the user here (ProtectedRoute), or to the home page.
  const returnTo = location.state?.from ?? '/';
  if (user) return <Navigate to={returnTo} replace />;

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(username, password);
      navigate(returnTo, { replace: true });
    } catch (err) {
      // 401 - the server does not say whether the username or the password is wrong.
      setError(err.status === 401 ? 'שם משתמש או סיסמה שגויים.' : err.message);
      setSubmitting(false);
    }
  }

  return (
    <div className="card form-card">
      <h1>התחברות</h1>
      {location.state?.expired && <div className="alert alert-info">תוקף ההתחברות פג. התחברו שוב כדי להמשיך.</div>}
      <form onSubmit={handleSubmit}>
        <label>
          שם משתמש
          <input value={username} onChange={(e) => setUsername(e.target.value)} required autoFocus />
        </label>
        <label>
          סיסמה
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>
        <ErrorMessage error={error} />
        <button className="btn btn-primary" disabled={submitting}>
          {submitting ? 'מתחבר...' : 'התחברות'}
        </button>
      </form>
      <p className="form-footer">
        עוד לא רשומים? <Link to="/register" state={location.state}>להרשמה</Link>
      </p>
    </div>
  );
}
