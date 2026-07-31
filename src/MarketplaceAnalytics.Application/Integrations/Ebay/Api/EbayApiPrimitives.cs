namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api;

public sealed record EbayMoney(decimal Amount, string Currency);

public sealed record EbayDateRange
{
    public EbayDateRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (start > end)
        {
            throw new ArgumentException("The date-range start must not be later than its end.");
        }

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
}
