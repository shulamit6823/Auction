using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.Api.Controllers;

using Auction.Core.DTOs;
using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

[Authorize(Roles = nameof(UserRole.Admin))] // the user list is for Admin only
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;

    public UsersController(IUserService userService, IMapper mapper)
    {
        _userService = userService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = _mapper.Map<IEnumerable<UserResponse>>(await _userService.GetAllAsync(cancellationToken));
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(_mapper.Map<UserResponse>(await _userService.GetByIdAsync(id, cancellationToken)));
        }
        catch (NotFoundException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }
}
