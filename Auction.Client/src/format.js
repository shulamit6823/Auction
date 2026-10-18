export function formatPrice(amount) {
  return `₪${Number(amount).toLocaleString('he-IL')}`;
}

export function formatDate(value) {
  return value ? new Date(value).toLocaleString('he-IL', { dateStyle: 'short', timeStyle: 'short' }) : '-';
}

// "2 ימים 3 שעות", "5 דק' 10 שנ'" - only for display; the server decides when an auction ends.
export function formatTimeLeft(endsAt, now = new Date()) {
  const ms = new Date(endsAt) - now;
  if (ms <= 0) return null;

  const minutes = Math.floor(ms / 60000);
  const days = Math.floor(minutes / 1440);
  const hours = Math.floor((minutes % 1440) / 60);
  if (days > 0) return `${days === 1 ? 'יום' : `${days} ימים`} ${hours} שעות`;
  if (hours > 0) return `${hours} שעות ${minutes % 60} דק'`;
  return `${minutes} דק' ${Math.floor((ms % 60000) / 1000)} שנ'`;
}
