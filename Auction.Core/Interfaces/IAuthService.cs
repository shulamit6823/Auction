namespace Auction.Core.Interfaces;

using Auction.Core.Models;

public interface IAuthService
{
    // Returns the user and a new JWT when the username and password are correct, otherwise null.
    // Deliberately does not say which of the two was wrong.
    Task<LoginResult?> LoginAsync(string username, string password);

    // Creates a regular User and logs them in right away (returns a JWT, like Login).
    // Throws BusinessRuleException for invalid input or a taken username/email.
    Task<LoginResult> RegisterAsync(string username, string email, string password);
}
