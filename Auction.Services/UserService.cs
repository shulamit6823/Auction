namespace Auction.Services;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<User> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            throw new NotFoundException($"משתמש {id} לא נמצא.");

        return user;
    }
}
