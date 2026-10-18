const labels = { Pending: 'ממתינה לאישור', Active: 'פעילה', Closed: 'נסגרה', Rejected: 'נדחתה' };

export default function StatusBadge({ status }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{labels[status] ?? status}</span>;
}
