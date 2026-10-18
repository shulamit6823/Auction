using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.Api.Controllers;

using Auction.Core.DTOs;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;

    public AuthController(IAuthService authService, IMapper mapper)
    {
        _authService = authService;
        _mapper = mapper;
    }

    [AllowAnonymous] // the user is not logged in yet
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password);
        if (result == null)
            return UnauthorizedProblem("שם משתמש או סיסמה שגויים.");

        return Ok(_mapper.Map<LoginResponse>(result));
    }

    [AllowAnonymous] // the user does not have an account yet
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            return StatusCode(StatusCodes.Status201Created, _mapper.Map<LoginResponse>(result));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequestProblem(ex.Message);
        }
    }
}
