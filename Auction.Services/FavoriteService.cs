namespace Auction.Services;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUserRepository _userRepository;

    public FavoriteService(IFavoriteRepository favoriteRepository, IAuctionRepository auctionRepository, IUserRepository userRepository)
    {
        _favoriteRepository = favoriteRepository;
        _auctionRepository = auctionRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<Favorite>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.GetByIdAsync(userId, cancellationToken) == null)
            throw new NotFoundException($"משתמש {userId} לא נמצא.");

        return await _favoriteRepository.GetByUserAsync(userId, cancellationToken);
    }

    public async Task AddAsync(int userId, int auctionId)
    {
        if (await _userRepository.GetByIdAsync(userId) == null)
            throw new NotFoundException($"משתמש {userId} לא נמצא.");
        var auction = await _auctionRepository.GetByIdAsync(auctionId);
        if (auction == null || !AuctionVisibility.CanView(auction, userId, viewerIsAdmin: false))
            throw new NotFoundException($"מכירה {auctionId} לא נמצאה.");
        if (await _favoriteRepository.GetAsync(userId, auctionId) != null)
            throw new BusinessRuleException("המכירה כבר נמצאת במועדפים שלך.");

        _favoriteRepository.Add(new Favorite { UserId = userId, AuctionId = auctionId });
        await _favoriteRepository.SaveAsync();
    }

    public async Task RemoveAsync(int userId, int auctionId)
    {
        var favorite = await _favoriteRepository.GetAsync(userId, auctionId);
        if (favorite == null)
            throw new NotFoundException("המכירה לא נמצאת במועדפים שלך.");

        _favoriteRepository.Remove(favorite);
        await _favoriteRepository.SaveAsync();
    }
}
