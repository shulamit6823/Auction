import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/api';
import { useLoad } from '../useLoad';
import AuctionCard from '../components/AuctionCard';
import { Empty, ErrorMessage, Loading } from '../components/Messages';

export default function MyAuctionsPage() {
  const { data: auctions, loading, error, reload } = useLoad(api.getMyAuctions, []);
  const [actionError, setActionError] = useState(null);

  async function cancel(auction) {
    if (!window.confirm(`לבטל את המכירה "${auction.title}"? היא תימחק.`)) return;
    setActionError(null);
    try {
      await api.cancelAuction(auction.id);
    } catch (err) {
      setActionError(err.message);
    }
    reload();
  }

  return (
    <>
      <div className="page-header">
        <h1>המכירות שלי</h1>
        <Link to="/auctions/new" className="btn btn-primary">+ מכירה חדשה</Link>
      </div>
      {loading ? <Loading /> : <ErrorMessage error={error ?? actionError} />}
      {auctions && (auctions.length === 0
        ? <Empty text="עדיין לא יצרת מכירות." />
        : (
          <div className="grid">
            {auctions.map((a) => {
              // Before approval the seller may still edit or cancel (the server checks it again).
              const editable = a.status === 'Pending' || a.status === 'Rejected';
              return (
                <AuctionCard key={a.id} auction={a}>
                  {a.status === 'Rejected' && <p className="rejection">סיבת הדחייה: {a.rejectionReason}</p>}
                  {editable && (
                    <div className="button-row">
                      <Link to={`/auctions/${a.id}/edit`} className="btn btn-outline btn-small">עריכה</Link>
                      <button className="btn btn-danger btn-small" onClick={() => cancel(a)}>ביטול המכירה</button>
                    </div>
                  )}
                </AuctionCard>
              );
            })}
          </div>
        ))}
    </>
  );
}
