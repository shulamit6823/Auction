using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.Api.Controllers;

using Auction.Core.DTOs;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

[Route("api/auctions/{auctionId:int}/bids")]
public class BidsController : ApiControllerBase
{
    private readonly IBidService _bidService;
    private readonly IMapper _mapper;

    public BidsController(IBidService bidService, IMapper mapper)
    {
        _bidService = bidService;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BidResponse>>> GetByAuction([FromRoute] int auctionId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(_mapper.Map<IEnumerable<BidResponse>>(await _bidService.GetByAuctionAsync(auctionId, CurrentUserId, IsAdmin, cancellationToken)));
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }

    // The route starts with "/", so it does not get the controller's "api/auctions/{auctionId}/bids" prefix.
    [HttpGet("/api/bids/mine")]
    public async Task<ActionResult<IEnumerable<MyBidResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        return Ok(_mapper.Map<IEnumerable<MyBidResponse>>(await _bidService.GetMyBidsAsync(userId, cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<BidResponse>> PlaceBid([FromRoute] int auctionId, [FromBody] PlaceBidRequest request)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            var bid = await _bidService.PlaceBidAsync(auctionId, userId, request.Amount, request.Version);
            return StatusCode(StatusCodes.Status201Created, _mapper.Map<BidResponse>(bid));
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
        catch (ConcurrencyConflictException ex)
        {
            return ConflictProblem(ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequestProblem(ex.Message);
        }
    }
}
