using Microsoft.EntityFrameworkCore;

namespace Auction.Data.Repositories;

using Auction.Core.Entities;
using Auction.Core.Interfaces;

public class UserRepository : IUserRepository
{
    private readonly AuctionDbContext _context;

    public UserRepository(AuctionDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.OrderBy(u => u.Username).ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
