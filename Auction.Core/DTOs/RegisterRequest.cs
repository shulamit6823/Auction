namespace Auction.Core.DTOs;

// No Role here: a new user is always a regular User. Only the server decides who is Admin.
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
