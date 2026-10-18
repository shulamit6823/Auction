// All calls to the server go through request(): it adds the JWT and turns an error response
// (ProblemDetails JSON from the server) into an ApiError with the status code and the message.

export const TOKEN_KEY = 'auction.session';

export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.status = status;
  }
}

// Set by AuthProvider: called when the server rejects our token (expired or invalid).
let onSessionExpired = () => {};
export function setSessionExpiredHandler(handler) {
  onSessionExpired = handler;
}

function getToken() {
  const saved = localStorage.getItem(TOKEN_KEY);
  return saved ? JSON.parse(saved).token : null;
}

async function request(method, url, body) {
  const headers = {};
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response;
  try {
    response = await fetch(url, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined
    });
  } catch {
    throw new ApiError(0, 'אין חיבור לשרת. ודאו שה-API רץ.');
  }

  if (!response.ok) {
    // 401 although we sent a token = the token is no longer valid -> log out.
    if (response.status === 401 && token) onSessionExpired();
    const problem = await response.json().catch(() => null);
    throw new ApiError(response.status, errorMessage(response.status, problem));
  }

  // 204 No Content has no body.
  return response.status === 204 ? null : response.json();
}

function errorMessage(status, problem) {
  // The automatic 400 of [ApiController] (e.g. a field with the wrong type) has English
  // framework messages in "errors" - show one Hebrew message instead.
  if (problem?.errors) return 'הנתונים שנשלחו אינם תקינים. בדקו את השדות ונסו שוב.';
  if (problem?.detail) return problem.detail;

  switch (status) {
    case 401: return 'צריך להתחבר מחדש.';
    case 403: return 'אין לך הרשאה לפעולה הזו.';
    case 404: return 'לא נמצא.';
    default: return 'אירעה שגיאה. נסו שוב.';
  }
}

export const api = {
  login: (username, password) => request('POST', '/api/auth/login', { username, password }),
  register: (username, email, password) => request('POST', '/api/auth/register', { username, email, password }),

  // search and sort go in the query string: /api/auctions?search=bike&sort=PriceLow
  getActiveAuctions: (search = '', sort = 'EndingSoon') =>
    request('GET', `/api/auctions?${new URLSearchParams({ search, sort })}`),
  getAllAuctions: () => request('GET', '/api/auctions/all'),
  getMyAuctions: () => request('GET', '/api/auctions/mine'),
  getAuction: (id) => request('GET', `/api/auctions/${id}`),
  createAuction: (auction) => request('POST', '/api/auctions', auction),
  updateAuction: (id, auction) => request('PUT', `/api/auctions/${id}`, auction),
  cancelAuction: (id) => request('DELETE', `/api/auctions/mine/${id}`),
  approveAuction: (id) => request('PUT', `/api/auctions/${id}/approve`),
  rejectAuction: (id, reason) => request('PUT', `/api/auctions/${id}/reject`, { reason }),
  deleteAuction: (id) => request('DELETE', `/api/auctions/${id}`),

  getBids: (auctionId) => request('GET', `/api/auctions/${auctionId}/bids`),
  getMyBids: () => request('GET', '/api/bids/mine'),
  // version = the auction Version the user saw. If someone bid in between, the server returns 409.
  placeBid: (auctionId, amount, version) => request('POST', `/api/auctions/${auctionId}/bids`, { amount, version }),

  getFavorites: () => request('GET', '/api/favorites'),
  addFavorite: (auctionId) => request('POST', `/api/favorites/${auctionId}`),
  removeFavorite: (auctionId) => request('DELETE', `/api/favorites/${auctionId}`),

  getUsers: () => request('GET', '/api/users')
};
