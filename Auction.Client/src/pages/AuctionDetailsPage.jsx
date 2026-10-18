import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/api';
import { useAuth } from '../auth/AuthContext';
import { useLoad } from '../useLoad';
import StatusBadge from '../components/StatusBadge';
import { ErrorMessage, Loading } from '../components/Messages';
import { formatDate, formatPrice, formatTimeLeft } from '../format';

export default function AuctionDetailsPage() {
  const { id } = useParams();
  const { user } = useAuth();

  const { data, loading, error, reload } = useLoad(async () => {
    const [auction, bids] = await Promise.all([api.getAuction(id), api.getBids(id)]);
    const favorites = user ? await api.getFavorites() : [];
    return { auction, bids, isFavorite: favorites.some((f) => f.id === auction.id) };
  }, [id, user]);

  const now = useNow();

  if (loading) return <Loading />;
  if (error) return <ErrorMessage error={error} />;

  const { auction, bids, isFavorite } = data;
  const timeLeft = formatTimeLeft(auction.endsAt, now);
  const isOpen = auction.status === 'Active' && timeLeft !== null;
  const sortedBids = [...bids].sort((a, b) => b.amount - a.amount);

  return (
    <div className="details">
      <Link to="/" className="muted small">→ חזרה לרשימה</Link>

      <div className="card">
        <div className="card-top">
          <StatusBadge status={auction.status} />
          {user && <FavoriteButton auctionId={auction.id} isFavorite={isFavorite} onChange={reload} />}
        </div>
        <h1>{auction.title}</h1>
        <p>{auction.description}</p>

        <div className="facts">
          <Fact label="מחיר נוכחי" value={<span className="price">{formatPrice(auction.currentPrice)}</span>} />
          <Fact label="מחיר פתיחה" value={formatPrice(auction.startingPrice)} />
          <Fact label="מוכר" value={auction.sellerUsername} />
          <Fact label="מסתיימת" value={formatDate(auction.endsAt)} />
          {isOpen && <Fact label="נותרו" value={timeLeft} />}
        </div>

        <AuctionState auction={auction} isOpen={isOpen} bids={sortedBids} user={user} />
      </div>

      {isOpen && <BidForm auction={auction} user={user} onBidPlaced={reload} />}

      <div className="card">
        <h2>היסטוריית הצעות ({bids.length})</h2>
        {sortedBids.length === 0 ? (
          <p className="muted">עדיין אין הצעות.</p>
        ) : (
          <table>
            <thead>
              <tr><th>מציע</th><th>סכום</th><th>זמן</th></tr>
            </thead>
            <tbody>
              {sortedBids.map((b) => (
                <tr key={b.id} className={b.bidderId === user?.id ? 'mine' : undefined}>
                  <td>{b.bidderUsername}</td>
                  <td>{formatPrice(b.amount)}</td>
                  <td>{formatDate(b.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

// The current time, updated every second - only for the countdown.
function useNow() {
  const [now, setNow] = useState(new Date());
  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);
  return now;
}

function Fact({ label, value }) {
  return (
    <div className="fact">
      <span className="muted small">{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

// The winner and the status come from the server. The client only shows them.
function AuctionState({ auction, isOpen, bids, user }) {
  // Only the seller or an Admin can see a Pending / Rejected auction (the server returns 404 to others).
  const editLink = auction.sellerId === user?.id && <> <Link to={`/auctions/${auction.id}/edit`}>לעריכת המכירה</Link></>;

  if (auction.status === 'Pending')
    return <div className="alert alert-info">המכירה ממתינה לאישור מנהל. הזמן יתחיל לרוץ מרגע האישור.{editLink}</div>;

  if (auction.status === 'Rejected')
    return <div className="alert alert-error">המנהל דחה את המכירה: {auction.rejectionReason}{editLink}</div>;

  if (auction.status === 'Active' && !isOpen)
    return <div className="alert alert-info">הזמן הסתיים. המכירה תיסגר בדקות הקרובות והזוכה ייקבע.</div>;

  if (auction.status !== 'Closed') return null;

  if (!auction.winnerId)
    return <div className="alert alert-info">המכירה נסגרה ללא הצעות.</div>;

  const winnerName = bids.find((b) => b.bidderId === auction.winnerId)?.bidderUsername;
  return (
    <div className="alert alert-winner">
      {auction.winnerId === user?.id
        ? `זכית במכירה! מחיר סופי: ${formatPrice(auction.currentPrice)}`
        : `הזוכה: ${winnerName} - מחיר סופי: ${formatPrice(auction.currentPrice)}`}
    </div>
  );
}

function FavoriteButton({ auctionId, isFavorite, onChange }) {
  const [error, setError] = useState(null);

  async function toggle() {
    setError(null);
    try {
      if (isFavorite) await api.removeFavorite(auctionId);
      else await api.addFavorite(auctionId);
      onChange();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div>
      <button className={`btn ${isFavorite ? 'btn-accent' : 'btn-outline'}`} onClick={toggle}>
        {isFavorite ? '★ במועדפים' : '☆ הוספה למועדפים'}
      </button>
      <ErrorMessage error={error} />
    </div>
  );
}

function BidForm({ auction, user, onBidPlaced }) {
  const [amount, setAmount] = useState('');
  const [message, setMessage] = useState(null); // { type: 'success' | 'conflict' | 'error', text }
  const [submitting, setSubmitting] = useState(false);

  if (!user)
    return (
      <div className="card">
        <Link to="/login" state={{ from: `/auctions/${auction.id}` }}>התחברו</Link> או{' '}
        <Link to="/register" state={{ from: `/auctions/${auction.id}` }}>הירשמו</Link> כדי להציע.
      </div>
    );
  if (user.id === auction.sellerId)
    return <div className="card muted">זו המכירה שלך - אי אפשר להציע עליה.</div>;

  async function handleSubmit(e) {
    e.preventDefault();
    setMessage(null);
    setSubmitting(true);
    try {
      // auction.version = the version this page shows. The server compares it to the DB.
      await api.placeBid(auction.id, Number(amount), auction.version);
      setMessage({ type: 'success', text: `ההצעה שלך (${formatPrice(amount)}) התקבלה!` });
      setAmount('');
    } catch (err) {
      if (err.status === 409) {
        // Someone else bid after this page was loaded. No automatic retry:
        // load the new price and let the user decide again.
        setMessage({ type: 'conflict', text: 'מישהו אחר הציע ממש עכשיו, וההצעה שלך לא נקלטה. המחיר עודכן - בדקו אותו והציעו שוב.' });
      } else {
        setMessage({ type: 'error', text: err.message });
      }
    }
    setSubmitting(false);
    onBidPlaced(); // reload: new price, new version, new bid list
  }

  return (
    <div className="card">
      <h2>הגשת הצעה</h2>
      <form onSubmit={handleSubmit} className="bid-form">
        {/* No min = current price here: the "higher than the current price" rule is checked by the server. */}
        <input
          type="number"
          min="0"
          step="any"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          placeholder={`יותר מ-${formatPrice(auction.currentPrice)}`}
          required
        />
        <button className="btn btn-primary" disabled={submitting}>
          {submitting ? 'שולח...' : 'הצעה'}
        </button>
      </form>
      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
    </div>
  );
}
