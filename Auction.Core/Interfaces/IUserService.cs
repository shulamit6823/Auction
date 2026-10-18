namespace Auction.Core.Interfaces;

using Auction.Core.Entities;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
