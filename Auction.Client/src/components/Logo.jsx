import { Link } from 'react-router-dom';

// The site logo: a gavel (auction hammer) on a badge in the two brand colors, plus the name.
export default function Logo() {
  return (
    <Link to="/" className="logo" aria-label="דף הבית">
      <svg className="logo-mark" viewBox="0 0 48 48" aria-hidden="true">
        <defs>
          <linearGradient id="logo-gradient" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#d66eeb" />
            <stop offset="1" stopColor="#62e4eb" />
          </linearGradient>
        </defs>
        <rect width="48" height="48" rx="14" fill="url(#logo-gradient)" />
        <g fill="#0b1426" transform="rotate(-45 24 24)">
          <rect x="14" y="10" width="20" height="9" rx="3" />  {/* gavel head */}
          <rect x="22" y="18" width="4" height="18" rx="2" />   {/* handle */}
        </g>
        <rect x="11" y="35" width="18" height="4" rx="2" fill="#0b1426" />  {/* base */}
      </svg>
      <span className="logo-text">
        <strong>מכירות פומביות</strong>
        <small>מציעים. מנצחים.</small>
      </span>
    </Link>
  );
}
