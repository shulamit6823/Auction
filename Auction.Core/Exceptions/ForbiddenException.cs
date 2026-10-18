namespace Auction.Core.Exceptions;

// The user is logged in, but this action on this item is not theirs to do (e.g. editing someone else's auction).
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
