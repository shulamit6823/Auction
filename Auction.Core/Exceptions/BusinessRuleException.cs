namespace Auction.Core.Exceptions;

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
