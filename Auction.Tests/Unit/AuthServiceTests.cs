using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Auction.Tests.Unit;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => "hashed:" + p);
        _tokenService.Setup(t => t.CreateToken(It.IsAny<User>())).Returns(("token", DateTime.UtcNow.AddHours(1)));

        _service = new AuthService(_userRepository.Object, _passwordHasher.Object, _tokenService.Object,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Register_ValidInput_CreatesRegularUserWithHashedPassword()
    {
        var result = await _service.RegisterAsync(" newuser ", "new@test.com", "secret1");

        Assert.Equal("newuser", result.User.Username);
        Assert.Equal(UserRole.User, result.User.Role);
        Assert.Equal("hashed:secret1", result.User.PasswordHash);
        Assert.Equal("token", result.Token);
        _userRepository.Verify(r => r.Add(result.User), Times.Once);
        _userRepository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_TakenUsername_Throws()
    {
        _userRepository.Setup(r => r.GetByUsernameAsync("dana", It.IsAny<CancellationToken>())).ReturnsAsync(new User());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegisterAsync("dana", "other@test.com", "secret1"));
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_TakenEmail_Throws()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("dana@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(new User());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegisterAsync("someone", "dana@test.com", "secret1"));
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData("", "a@test.com", "secret1")]       // no username
    [InlineData("user", "not-an-email", "secret1")] // bad email
    [InlineData("user", "a@test.com", "12345")]     // password too short
    public async Task Register_InvalidInput_Throws(string username, string email, string password)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegisterAsync(username, email, password));
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }
}
