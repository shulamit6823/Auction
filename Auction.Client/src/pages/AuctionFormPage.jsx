import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/api';
import { ErrorMessage, Loading } from '../components/Messages';

const emptyForm = { title: '', description: '', startingPrice: '0', durationAmount: '24', durationUnit: 'Hours' };

// The server keeps only DurationHours - show it back in the largest unit that fits exactly.
function toForm(auction) {
  const hours = auction.durationHours;
  const [amount, unit] = hours % 168 === 0 ? [hours / 168, 'Weeks'] : hours % 24 === 0 ? [hours / 24, 'Days'] : [hours, 'Hours'];
  return {
    title: auction.title,
    description: auction.description,
    startingPrice: String(auction.startingPrice),
    durationAmount: String(amount),
    durationUnit: unit
  };
}

// One form for both: /auctions/new (create) and /auctions/:id/edit (the seller edits before approval).
export default function AuctionFormPage() {
  const { id } = useParams();
  const isEdit = id !== undefined;
  const navigate = useNavigate();
  const [form, setForm] = useState(emptyForm);
  const [rejectionReason, setRejectionReason] = useState(null);
  const [loading, setLoading] = useState(isEdit);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!isEdit) return;
    api.getAuction(id)
      .then((auction) => {
        setForm(toForm(auction));
        setRejectionReason(auction.rejectionReason);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [id, isEdit]);

  function update(field) {
    return (e) => setForm({ ...form, [field]: e.target.value });
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      // Only the fields of CreateAuctionRequest. The seller comes from the token on the server.
      const request = {
        title: form.title,
        description: form.description,
        startingPrice: Number(form.startingPrice),
        durationAmount: Number(form.durationAmount),
        durationUnit: form.durationUnit
      };
      const auction = isEdit ? await api.updateAuction(id, request) : await api.createAuction(request);
      navigate(`/auctions/${auction.id}`);
    } catch (err) {
      setError(err.message);
      setSubmitting(false);
    }
  }

  if (loading) return <Loading />;

  return (
    <div className="card form-card">
      <h1>{isEdit ? 'עריכת מכירה' : 'מכירה חדשה'}</h1>
      {rejectionReason && (
        <div className="alert alert-error">המנהל דחה את המכירה: {rejectionReason}<br />אחרי השמירה היא תישלח שוב לאישור.</div>
      )}
      <p className="muted small">המכירה תעלה לאתר אחרי אישור מנהל. הזמן מתחיל לרוץ מרגע האישור.</p>
      <form onSubmit={handleSubmit}>
        <label>
          שם המוצר
          <input value={form.title} onChange={update('title')} required maxLength={200} />
        </label>
        <label>
          תיאור
          <textarea value={form.description} onChange={update('description')} rows={4} />
        </label>
        <label>
          מחיר פתיחה (₪)
          <input type="number" min="0" step="any" value={form.startingPrice} onChange={update('startingPrice')} required />
        </label>
        <div className="row">
          <label>
            משך המכירה
            <input type="number" min="1" value={form.durationAmount} onChange={update('durationAmount')} required />
          </label>
          <label>
            יחידה
            <select value={form.durationUnit} onChange={update('durationUnit')}>
              <option value="Hours">שעות</option>
              <option value="Days">ימים</option>
              <option value="Weeks">שבועות</option>
            </select>
          </label>
        </div>
        <ErrorMessage error={error} />
        <button className="btn btn-primary" disabled={submitting}>
          {submitting ? 'שולח...' : isEdit ? 'שמירה ושליחה לאישור' : 'שליחה לאישור'}
        </button>
      </form>
    </div>
  );
}
