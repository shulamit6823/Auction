import { Link } from 'react-router-dom';
import { api } from '../api/api';
import { useLoad } from '../useLoad';
import StatusBadge from '../components/StatusBadge';
import { Empty, ErrorMessage, Loading } from '../components/Messages';
import { formatDate, formatPrice } from '../format';

// "Leading" comes from the server (IsLeading). On a closed auction, leading = won.
function standing(bid) {
  const closed = bid.auctionStatus === 'Closed';
  if (bid.isLeading) return closed ? { text: '🏆 זכית', className: 'standing-won' } : { text: 'אתם מובילים', className: 'standing-leading' };
  return closed ? { text: 'לא זכית', className: 'standing-lost' } : { text: 'עקפו אתכם', className: 'standing-outbid' };
}

export default function MyBidsPage() {
  const { data: bids, loading, error } = useLoad(api.getMyBids, []);

  return (
    <>
      <h1>ההצעות שלי</h1>
      {loading ? <Loading /> : <ErrorMessage error={error} />}
      {bids && (bids.length === 0 ? <Empty text="עדיין לא הצעת על אף מכירה." /> : (
        <div className="card table-card">
          <table>
            <thead>
              <tr><th>מכירה</th><th>סטטוס</th><th>ההצעה שלי</th><th>המחיר הנוכחי</th><th>מצב</th><th>מסתיימת</th><th></th></tr>
            </thead>
            <tbody>
              {bids.map((b) => {
                const s = standing(b);
                const canBidAgain = b.auctionStatus === 'Active' && !b.isLeading;
                return (
                  <tr key={b.auctionId}>
                    <td><Link to={`/auctions/${b.auctionId}`}>{b.auctionTitle}</Link></td>
                    <td><StatusBadge status={b.auctionStatus} /></td>
                    <td>{formatPrice(b.myHighestBid)}</td>
                    <td>{formatPrice(b.auctionCurrentPrice)}</td>
                    <td><span className={`standing ${s.className}`}>{s.text}</span></td>
                    <td>{formatDate(b.auctionEndsAt)}</td>
                    <td className="actions">
                      {canBidAgain && <Link to={`/auctions/${b.auctionId}`} className="btn btn-primary btn-small">להציע שוב</Link>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ))}
    </>
  );
}
