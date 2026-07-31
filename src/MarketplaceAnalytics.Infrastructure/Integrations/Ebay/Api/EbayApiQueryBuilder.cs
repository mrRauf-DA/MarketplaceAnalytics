using System.Globalization;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Finances;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Fulfillment;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api;

internal static class EbayApiQueryBuilder
{
    public static string Pagination(string path, EbayPageRequest page) =>
        Build(path, [("limit", page.Limit.ToString(CultureInfo.InvariantCulture)), ("offset", page.Offset.ToString(CultureInfo.InvariantCulture))]);

    public static string Orders(EbayOrderQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var filters = new List<string>();
        AddRange(filters, "creationdate", query.CreationDateRange);
        AddRange(filters, "lastmodifieddate", query.LastModifiedDateRange);
        AddValue(filters, "orderfulfillmentstatus", query.FulfillmentStatus);
        return WithFilters("sell/fulfillment/v1/order", query.Page, filters);
    }

    public static string Transactions(EbayTransactionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var filters = new List<string>();
        AddRange(filters, "transactionDate", query.TransactionDateRange);
        AddValue(filters, "transactionStatus", query.TransactionStatus);
        AddValue(filters, "transactionType", query.TransactionType);
        return WithFilters("sell/finances/v1/transaction", query.Page, filters);
    }

    public static string Payouts(EbayPayoutQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var filters = new List<string>();
        AddRange(filters, "payoutDate", query.PayoutDateRange);
        AddValue(filters, "payoutStatus", query.PayoutStatus);
        return WithFilters("sell/finances/v1/payout", query.Page, filters);
    }

    public static string EncodedPath(string prefix, string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return prefix + Uri.EscapeDataString(value);
    }

    private static string WithFilters(string path, EbayPageRequest page, IReadOnlyCollection<string> filters)
    {
        var values = new List<(string Key, string Value)>
        {
            ("limit", page.Limit.ToString(CultureInfo.InvariantCulture)),
            ("offset", page.Offset.ToString(CultureInfo.InvariantCulture))
        };
        if (filters.Count > 0)
        {
            values.Add(("filter", string.Join(',', filters)));
        }

        return Build(path, values);
    }

    private static string Build(string path, IEnumerable<(string Key, string Value)> values) =>
        path + "?" + string.Join('&', values.Select(value =>
            $"{Uri.EscapeDataString(value.Key)}={Uri.EscapeDataString(value.Value)}"));

    private static void AddRange(ICollection<string> filters, string name, EbayDateRange? range)
    {
        if (range is not null)
        {
            filters.Add($"{name}:[{FormatUtc(range.Start)}..{FormatUtc(range.End)}]");
        }
    }

    private static void AddValue(ICollection<string> filters, string name, string? value)
    {
        if (value is null)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
        {
            throw new ArgumentException(
                "An eBay filter value may contain only ASCII letters, digits, underscores, and hyphens.",
                name);
        }

        filters.Add($"{name}:{{{value}}}");
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
}
