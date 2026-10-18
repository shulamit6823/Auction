namespace Auction.Core.Interfaces;

using Auction.Core.Entities;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);

    // Only marks the entity in the Change Tracker (no DB access) - stays sync.
    void Add(User user);
}
