import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/api';
import { useLoad } from '../useLoad';
import StatusBadge from '../components/StatusBadge';
import { Empty, ErrorMessage, Loading } from '../components/Messages';
import { formatDate, formatPrice } from '../format';

const filters = [
  { status: 'Pending', label: 'ממתינות לאישור' },
  { status: 'Active', label: 'פעילות' },
  { status: 'Closed', label: 'סגורות' },
  { status: 'Rejected', label: 'נדחו' },
  { status: null, label: 'הכל' }
];

export default function AdminAuctionsPage() {
  const { data: auctions, loading, error, reload } = useLoad(api.getAllAuctions, []);
  const [filter, setFilter] = useState('Pending');
  const [actionError, setActionError] = useState(null);

  async function run(action) {
    setActionError(null);
    try {
      await action();
    } catch (err) {
      setActionError(err.message);
    }
    reload();
  }

  function reject(auction) {
    const reason = window.prompt(`סיבת הדחייה של "${auction.title}" (המוכר יראה אותה):`);
    if (reason === null) return; // cancelled
    run(() => api.rejectAuction(auction.id, reason));
  }

  function remove(auction) {
    if (window.confirm(`למחוק את "${auction.title}"?`)) run(() => api.deleteAuction(auction.id));
  }

  const shown = auctions?.filter((a) => filter === null || a.status === filter) ?? [];

  return (
    <>
      <h1>ניהול מכירות</h1>

      <div className="tabs">
        {filters.map((f) => (
          <button key={f.label} className={filter === f.status ? 'tab active' : 'tab'} onClick={() => setFilter(f.status)}>
            {f.label} ({auctions?.filter((a) => f.status === null || a.status === f.status).length ?? 0})
          </button>
        ))}
      </div>

      {loading ? <Loading /> : <ErrorMessage error={error ?? actionError} />}
      {auctions && (shown.length === 0 ? <Empty text="אין מכירות להצגה." /> : (
        <div className="card table-card">
          <table>
            <thead>
              <tr><th>#</th><th>מוצר</th><th>מוכר</th><th>סטטוס</th><th>מחיר</th><th>נוצרה</th><th>מסתיימת</th><th></th></tr>
            </thead>
            <tbody>
              {shown.map((a) => (
                <tr key={a.id}>
                  <td>{a.id}</td>
                  <td><Link to={`/auctions/${a.id}`}>{a.title}</Link></td>
                  <td>{a.sellerUsername}</td>
                  <td><StatusBadge status={a.status} /></td>
                  <td>{formatPrice(a.currentPrice)}</td>
                  <td>{formatDate(a.createdAt)}</td>
                  <td>{formatDate(a.endsAt)}</td>
                  <td className="actions">
                    {a.status === 'Pending' && (
                      <>
                        <button className="btn btn-primary btn-small" onClick={() => run(() => api.approveAuction(a.id))}>אישור</button>
                        <button className="btn btn-outline btn-small" onClick={() => reject(a)}>דחייה</button>
                      </>
                    )}
                    {/* The server refuses to delete an active auction - the button only reflects that rule. */}
                    <button
                      className="btn btn-danger btn-small"
                      disabled={a.status === 'Active'}
                      title={a.status === 'Active' ? 'אי אפשר למחוק מכירה פעילה' : undefined}
                      onClick={() => remove(a)}
                    >
                      מחיקה
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </>
  );
}
