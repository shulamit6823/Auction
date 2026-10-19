using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.Api.Controllers;

using Auction.Core.DTOs;
using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Core.Models;

[Route("api/auctions")]
public class AuctionsController : ApiControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly IMapper _mapper;

    public AuctionsController(IAuctionService auctionService, IMapper mapper)
    {
        _auctionService = auctionService;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpGet]
    // Filter and sort come from the query string, e.g. GET api/auctions?search=bike&sort=PriceLow
    public async Task<ActionResult<IEnumerable<AuctionResponse>>> GetActive([FromQuery] string? search, [FromQuery] AuctionSort sort,
        CancellationToken cancellationToken)
    {
        var auctions = _mapper.Map<IEnumerable<AuctionResponse>>(await _auctionService.GetActiveAsync(search, sort, cancellationToken));
        return Ok(auctions);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<AuctionResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var auctions = _mapper.Map<IEnumerable<AuctionResponse>>(await _auctionService.GetAllAsync(cancellationToken));
        return Ok(auctions);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<AuctionResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        var auctions = _mapper.Map<IEnumerable<AuctionResponse>>(await _auctionService.GetBySellerAsync(userId, cancellationToken));
        return Ok(auctions);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuctionResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(_mapper.Map<AuctionResponse>(await _auctionService.GetVisibleByIdAsync(id, CurrentUserId, IsAdmin, cancellationToken)));
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<AuctionResponse>> Create([FromBody] CreateAuctionRequest request)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            var auction = await _auctionService.CreateAsync(userId, request.Title, request.Description,
                request.StartingPrice, request.DurationAmount, request.DurationUnit);
            return CreatedAtAction(nameof(GetById), new { id = auction.Id }, _mapper.Map<AuctionResponse>(auction));
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

    // Contact details of the seller and the winner of a closed auction - only for the two of them and an Admin.
    [HttpGet("{id:int}/contact")]
    public async Task<ActionResult<AuctionContactResponse>> GetContact([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            return Ok(_mapper.Map<AuctionContactResponse>(await _auctionService.GetContactAsync(id, userId, IsAdmin, cancellationToken)));
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenProblem(ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequestProblem(ex.Message);
        }
    }

    // The seller edits their auction before it is approved. Same fields as Create.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AuctionResponse>> Update([FromRoute] int id, [FromBody] CreateAuctionRequest request)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            var auction = await _auctionService.UpdateAsync(id, userId, request.Title, request.Description,
                request.StartingPrice, request.DurationAmount, request.DurationUnit);
            return Ok(_mapper.Map<AuctionResponse>(auction));
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenProblem(ex.Message);
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

    // The seller cancels their own auction before it is approved (the Admin delete is DELETE api/auctions/{id}).
    [HttpDelete("mine/{id:int}")]
    public async Task<IActionResult> Cancel([FromRoute] int id)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        try
        {
            await _auctionService.CancelAsync(id, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenProblem(ex.Message);
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

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}/reject")]
    public async Task<ActionResult<AuctionResponse>> Reject([FromRoute] int id, [FromBody] RejectAuctionRequest request)
    {
        try
        {
            return Ok(_mapper.Map<AuctionResponse>(await _auctionService.RejectAsync(id, request.Reason)));
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

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult<AuctionResponse>> Approve([FromRoute] int id)
    {
        try
        {
            return Ok(_mapper.Map<AuctionResponse>(await _auctionService.ApproveAsync(id)));
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

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        try
        {
            await _auctionService.DeleteAsync(id);
            return NoContent();
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
