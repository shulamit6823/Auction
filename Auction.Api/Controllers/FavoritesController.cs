using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace Auction.Api.Controllers;

using Auction.Core.DTOs;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

[Route("api/favorites")]
public class FavoritesController : ApiControllerBase
{
    private readonly IFavoriteService _favoriteService;
    private readonly IMapper _mapper;

    public FavoritesController(IFavoriteService favoriteService, IMapper mapper)
    {
        _favoriteService = favoriteService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuctionResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            var favorites = await _favoriteService.GetByUserAsync(userId, cancellationToken);
            var auctions = _mapper.Map<IEnumerable<AuctionResponse>>(favorites.Select(f => f.Auction));
            return Ok(auctions);
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }

    [HttpPost("{auctionId:int}")]
    public async Task<IActionResult> Add([FromRoute] int auctionId)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            await _favoriteService.AddAsync(userId, auctionId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequestProblem(ex.Message);
        }
    }

    [HttpDelete("{auctionId:int}")]
    public async Task<IActionResult> Remove([FromRoute] int auctionId)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            await _favoriteService.RemoveAsync(userId, auctionId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }
}
