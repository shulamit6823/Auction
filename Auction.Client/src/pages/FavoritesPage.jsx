import { useState } from 'react';
import { api } from '../api/api';
import { useLoad } from '../useLoad';
import AuctionCard from '../components/AuctionCard';
import { Empty, ErrorMessage, Loading } from '../components/Messages';

export default function FavoritesPage() {
  const { data: auctions, loading, error, reload } = useLoad(api.getFavorites, []);
  const [actionError, setActionError] = useState(null);

  async function remove(auctionId) {
    setActionError(null);
    try {
      await api.removeFavorite(auctionId);
      reload();
    } catch (err) {
      setActionError(err.message);
    }
  }

  return (
    <>
      <h1>מועדפים</h1>
      {loading ? <Loading /> : <ErrorMessage error={error ?? actionError} />}
      {auctions && (auctions.length === 0
        ? <Empty text="אין מכירות במועדפים. אפשר להוסיף מדף המכירה." />
        : (
          <div className="grid">
            {auctions.map((a) => (
              <AuctionCard key={a.id} auction={a}>
                <button className="btn btn-outline" onClick={() => remove(a.id)}>הסרה מהמועדפים</button>
              </AuctionCard>
            ))}
          </div>
        ))}
    </>
  );
}
