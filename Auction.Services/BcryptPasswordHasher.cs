namespace Auction.Services;

using Auction.Core.Interfaces;

public class BcryptPasswordHasher : IPasswordHasher
{
    // BCrypt creates a random salt for every hash and stores it inside the hash string,
    // so no separate salt column is needed.
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        // A user without a stored hash can never log in.
        if (string.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // The stored value is not a valid BCrypt hash - treat as a wrong password.
            return false;
        }
    }
}
