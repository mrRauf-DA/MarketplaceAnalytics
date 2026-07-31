using MarketplaceAnalytics.Application.Integrations.Ebay.Api;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Finances;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Transport;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Finances;

internal sealed class EbayFinancesClient(HttpClient httpClient, IEbayUserAccessTokenProvider tokenProvider)
    : EbayApiClientBase(httpClient, tokenProvider), IEbayFinancesClient
{
    public async Task<EbayApiPage<EbayTransaction>> GetTransactionsAsync(EbayTransactionQuery query, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<EbayTransactionPageDto>(EbayApiQueryBuilder.Transactions(query), EbayApiScopes.Finances, "GetTransactions", cancellationToken);
        return new EbayApiPage<EbayTransaction>((response.Transactions ?? []).Select(Map).ToArray(), response.Total, response.Limit, response.Offset, ToUri(response.Next), ToUri(response.Prev));
    }

    public async Task<EbayApiPage<EbayPayout>> GetPayoutsAsync(EbayPayoutQuery query, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<EbayPayoutPageDto>(EbayApiQueryBuilder.Payouts(query), EbayApiScopes.Finances, "GetPayouts", cancellationToken);
        return new EbayApiPage<EbayPayout>((response.Payouts ?? []).Select(Map).ToArray(), response.Total, response.Limit, response.Offset, ToUri(response.Next), ToUri(response.Prev));
    }

    private static EbayTransaction Map(EbayTransactionDto value) => new(RequiredIdentifier(value.TransactionId, "transaction ID", "MapTransaction"), value.TransactionType, value.TransactionStatus, value.OrderId, value.LineItemId, value.PayoutId, value.BookingEntry, value.TransactionDate, ToMoney(value.Amount), ToMoney(value.TotalFeeBasisAmount), ToMoney(value.TotalFeeAmount), (value.OrderLineItems ?? []).Select(item => new EbayTransactionLineItem(item.LineItemId, ToMoney(item.FeeBasisAmount))).ToArray());
    private static EbayPayout Map(EbayPayoutDto value) => new(RequiredIdentifier(value.PayoutId, "payout ID", "MapPayout"), value.PayoutStatus, ToMoney(value.Amount), value.PayoutDate, value.LastAttemptedPayoutDate, value.TransactionCount);
}
