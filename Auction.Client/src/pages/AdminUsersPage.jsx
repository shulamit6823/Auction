import { api } from '../api/api';
import { useLoad } from '../useLoad';
import { ErrorMessage, Loading } from '../components/Messages';

// All registered users (not only the ones online) - GET api/users, Admin only.
export default function AdminUsersPage() {
  const { data: users, loading, error } = useLoad(api.getUsers, []);

  return (
    <>
      <h1>משתמשים</h1>
      {loading ? <Loading /> : <ErrorMessage error={error} />}
      {users && (
        <div className="card table-card">
          <table>
            <thead>
              <tr><th>#</th><th>שם משתמש</th><th>אימייל</th><th>תפקיד</th></tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td>{u.id}</td>
                  <td>{u.username}</td>
                  <td>{u.email}</td>
                  <td><span className={`badge ${u.role === 'Admin' ? 'badge-admin' : 'badge-user'}`}>{u.role === 'Admin' ? 'מנהל' : 'משתמש'}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
