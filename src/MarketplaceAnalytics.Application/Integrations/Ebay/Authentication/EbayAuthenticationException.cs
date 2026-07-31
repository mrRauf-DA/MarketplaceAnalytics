namespace MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

public sealed class EbayAuthenticationException : Exception
{
    public EbayAuthenticationException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string? ErrorCode { get; }
}
