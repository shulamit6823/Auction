import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ErrorMessage } from '../components/Messages';

export default function RegisterPage() {
  const { user, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState({ username: '', email: '', password: '', confirm: '' });
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const returnTo = location.state?.from ?? '/';
  if (user) return <Navigate to={returnTo} replace />;

  function update(field) {
    return (e) => setForm({ ...form, [field]: e.target.value });
  }

  async function handleSubmit(e) {
    e.preventDefault();
    // Only a convenience check - the server validates everything again.
    if (form.password !== form.confirm) {
      setError('הסיסמאות אינן זהות.');
      return;
    }

    setError(null);
    setSubmitting(true);
    try {
      await register(form.username, form.email, form.password);
      navigate(returnTo, { replace: true });
    } catch (err) {
      setError(err.message);
      setSubmitting(false);
    }
  }

  return (
    <div className="card form-card">
      <h1>הרשמה</h1>
      <form onSubmit={handleSubmit}>
        <label>
          שם משתמש
          <input value={form.username} onChange={update('username')} required maxLength={30} autoFocus />
        </label>
        <label>
          אימייל
          <input type="email" value={form.email} onChange={update('email')} required />
        </label>
        <label>
          סיסמה (לפחות 6 תווים)
          <input type="password" value={form.password} onChange={update('password')} required minLength={6} />
        </label>
        <label>
          אימות סיסמה
          <input type="password" value={form.confirm} onChange={update('confirm')} required />
        </label>
        <ErrorMessage error={error} />
        <button className="btn btn-primary" disabled={submitting}>
          {submitting ? 'נרשם...' : 'הרשמה'}
        </button>
      </form>
      <p className="form-footer">
        כבר רשומים? <Link to="/login" state={location.state}>להתחברות</Link>
      </p>
    </div>
  );
}
