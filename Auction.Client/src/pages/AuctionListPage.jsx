import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/api';
import { useAuth } from '../auth/AuthContext';
import { useLoad } from '../useLoad';
import AuctionCard from '../components/AuctionCard';
import { Empty, ErrorMessage, Loading } from '../components/Messages';
import { formatPrice } from '../format';

const sortOptions = [
  { value: 'EndingSoon', label: 'מסתיימות בקרוב' },
  { value: 'Newest', label: 'החדשות ביותר' },
  { value: 'PriceLow', label: 'מחיר: מהנמוך לגבוה' },
  { value: 'PriceHigh', label: 'מחיר: מהגבוה לנמוך' }
];

export default function AuctionListPage() {
  const { user } = useAuth();
  const [searchText, setSearchText] = useState('');
  const [search, setSearch] = useState(''); // the text actually sent to the server
  const [sort, setSort] = useState('EndingSoon');

  // Wait until the user stops typing for a moment, instead of a request on every key.
  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchText.trim()), 400);
    return () => clearTimeout(timer);
  }, [searchText]);

  // The server filters and sorts - the list reloads when search or sort change.
  const { data: auctions, loading, error } = useLoad(() => api.getActiveAuctions(search, sort), [search, sort]);

  const highestPrice = auctions?.length ? Math.max(...auctions.map((a) => a.currentPrice)) : 0;

  return (
    <>
      <section className="hero">
        <div className="hero-text">
          <h1>
            מוצאים את המציאה.
            <br />
            <span className="gradient-text">מציעים ומנצחים.</span>
          </h1>
          <p>
            {user
              ? `שלום ${user.username}, מה מוכרים או קונים היום?`
              : 'כל משתמש רשום יכול להציע על מכירות ולהעלות פריטים משלו.'}
          </p>
          <div className="hero-actions">
            {user ? (
              <>
                <Link to="/auctions/new" className="btn btn-primary btn-big">+ מכירה חדשה</Link>
                <Link to="/favorites" className="btn btn-outline btn-big">המועדפים שלי</Link>
              </>
            ) : (
              <>
                <Link to="/register" className="btn btn-primary btn-big">הרשמה חינם</Link>
                <Link to="/login" className="btn btn-outline btn-big">התחברות</Link>
              </>
            )}
          </div>
        </div>

        <div className="hero-stats">
          <div className="stat">
            <strong>{auctions?.length ?? '-'}</strong>
            <span>מכירות פעילות</span>
          </div>
          <div className="stat">
            <strong>{auctions ? formatPrice(highestPrice) : '-'}</strong>
            <span>ההצעה הגבוהה כרגע</span>
          </div>
        </div>
      </section>

      <h2 className="section-title">מכירות פעילות</h2>
      <div className="toolbar">
        <input
          type="search"
          className="search-box"
          placeholder="🔍 חיפוש לפי שם או תיאור..."
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <select value={sort} onChange={(e) => setSort(e.target.value)} aria-label="מיון">
          {sortOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
      </div>

      {loading ? <Loading /> : <ErrorMessage error={error} />}
      {auctions && (auctions.length === 0
        ? <Empty text={search ? `לא נמצאו מכירות עבור "${search}".` : 'אין כרגע מכירות פעילות.'} />
        : <div className="grid">{auctions.map((a) => <AuctionCard key={a.id} auction={a} />)}</div>)}
    </>
  );
}
