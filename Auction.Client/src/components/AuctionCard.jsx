import { Link } from 'react-router-dom';
import StatusBadge from './StatusBadge';
import { formatPrice, formatTimeLeft } from '../format';

export default function AuctionCard({ auction, children }) {
  const timeLeft = auction.status === 'Active' ? formatTimeLeft(auction.endsAt) : null;

  return (
    <div className="card auction-card">
      <div className="card-top">
        <StatusBadge status={auction.status} />
        {timeLeft && <span className="time-left">⏱ {timeLeft}</span>}
      </div>
      <Link to={`/auctions/${auction.id}`} className="card-title">{auction.title}</Link>
      <p className="muted small">מוכר: {auction.sellerUsername}</p>
      <div className="card-bottom">
        <div>
          <span className="muted small">מחיר נוכחי</span>
          <p className="price">{formatPrice(auction.currentPrice)}</p>
        </div>
        <Link to={`/auctions/${auction.id}`} className="btn btn-accent btn-small">
          {auction.status === 'Active' ? 'להצעה ←' : 'לפרטים ←'}
        </Link>
      </div>
      {children && <div className="card-actions">{children}</div>}
    </div>
  );
}
