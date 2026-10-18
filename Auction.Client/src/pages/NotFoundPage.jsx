import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="card form-card">
      <h1>הדף לא נמצא</h1>
      <Link to="/" className="btn btn-primary">לדף הבית</Link>
    </div>
  );
}
