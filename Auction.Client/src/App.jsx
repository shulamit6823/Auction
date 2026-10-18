import { Route, Routes } from 'react-router-dom';
import Navbar from './components/Navbar';
import ProtectedRoute from './components/ProtectedRoute';
import AuctionListPage from './pages/AuctionListPage';
import AuctionDetailsPage from './pages/AuctionDetailsPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import AuctionFormPage from './pages/AuctionFormPage';
import MyAuctionsPage from './pages/MyAuctionsPage';
import MyBidsPage from './pages/MyBidsPage';
import FavoritesPage from './pages/FavoritesPage';
import AdminAuctionsPage from './pages/AdminAuctionsPage';
import AdminUsersPage from './pages/AdminUsersPage';
import NotFoundPage from './pages/NotFoundPage';

export default function App() {
  return (
    <>
      <Navbar />
      <main className="container">
        <Routes>
          <Route path="/" element={<AuctionListPage />} />
          <Route path="/auctions/:id" element={<AuctionDetailsPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          <Route element={<ProtectedRoute />}>
            <Route path="/auctions/new" element={<AuctionFormPage />} />
            <Route path="/auctions/:id/edit" element={<AuctionFormPage />} />
            <Route path="/my-auctions" element={<MyAuctionsPage />} />
            <Route path="/my-bids" element={<MyBidsPage />} />
            <Route path="/favorites" element={<FavoritesPage />} />
          </Route>

          <Route element={<ProtectedRoute adminOnly />}>
            <Route path="/admin/auctions" element={<AdminAuctionsPage />} />
            <Route path="/admin/users" element={<AdminUsersPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>
    </>
  );
}
