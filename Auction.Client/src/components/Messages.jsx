export function ErrorMessage({ error }) {
  if (!error) return null;
  return <div className="alert alert-error">{error}</div>;
}

export function Loading() {
  return <p className="muted">טוען...</p>;
}

export function Empty({ text }) {
  return <p className="muted empty">{text}</p>;
}
