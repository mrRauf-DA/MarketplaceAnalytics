namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api;

public sealed record EbayApiError(long? ErrorId, string? Domain, string? Category, string? Subdomain, string? Message, string? LongMessage);

public sealed class EbayApiException : Exception
{
    public EbayApiException(string message, string operation, int statusCode, IReadOnlyList<EbayApiError>? errors = null, string? requestId = null, TimeSpan? retryAfter = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
        StatusCode = statusCode;
        Errors = errors ?? [];
        RequestId = requestId;
        RetryAfter = retryAfter;
    }

    public string Operation { get; }
    public int StatusCode { get; }
    public IReadOnlyList<EbayApiError> Errors { get; }
    public string? RequestId { get; }
    public TimeSpan? RetryAfter { get; }
}
