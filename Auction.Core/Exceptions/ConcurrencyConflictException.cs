namespace Auction.Core.Exceptions;

// The data was changed by someone else after it was read (Optimistic Concurrency).
// Defined in Core so the Services and the API do not depend on EF's DbUpdateConcurrencyException.
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
