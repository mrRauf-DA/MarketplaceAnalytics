namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api;

public sealed record EbayPageRequest
{
    public EbayPageRequest(int limit, int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        Limit = limit;
        Offset = offset;
    }

    public int Limit { get; }
    public int Offset { get; }
}

public sealed record EbayApiPage<T>(IReadOnlyList<T> Items, long? Total, int? Limit, int? Offset, Uri? Next, Uri? Previous);
