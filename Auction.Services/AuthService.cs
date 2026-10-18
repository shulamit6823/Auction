using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Auction.Services;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Core.Models;

public class AuthService : IAuthService
{
    private const int MinPasswordLength = 6;
    private const int MaxUsernameLength = 30;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    // Used when the username does not exist, so the answer takes the same time
    // as a wrong password and does not reveal which usernames exist.
    // Created once (on the first unknown username) and shared by all requests.
    private static string? _dummyHash;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResult?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            _dummyHash ??= _passwordHasher.Hash(Guid.NewGuid().ToString());
            _passwordHasher.Verify(password, _dummyHash);
            return null;
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            return null;

        return CreateLoginResult(user);
    }

    public async Task<LoginResult> RegisterAsync(string username, string email, string password)
    {
        username = username?.Trim() ?? string.Empty;
        email = email?.Trim() ?? string.Empty;

        if (username.Length == 0 || username.Length > MaxUsernameLength)
            throw new BusinessRuleException($"חובה להזין שם משתמש (עד {MaxUsernameLength} תווים).");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new BusinessRuleException("יש להזין אימייל תקין.");
        if (password == null || password.Length < MinPasswordLength)
            throw new BusinessRuleException($"הסיסמה חייבת להכיל לפחות {MinPasswordLength} תווים.");

        // Checked here for a clear message. The unique indexes in the DB are the final guard.
        if (await _userRepository.GetByUsernameAsync(username) != null)
            throw new BusinessRuleException("שם המשתמש הזה כבר תפוס.");
        if (await _userRepository.GetByEmailAsync(email) != null)
            throw new BusinessRuleException("האימייל הזה כבר רשום במערכת.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.User
        };
        _userRepository.Add(user);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User {UserId} registered: {Username}.", user.Id, user.Username);
        return CreateLoginResult(user);
    }

    private LoginResult CreateLoginResult(User user)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user);
        return new LoginResult { User = user, Token = token, ExpiresAt = expiresAt };
    }
}
