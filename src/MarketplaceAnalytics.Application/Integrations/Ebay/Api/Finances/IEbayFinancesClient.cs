namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Finances;

public interface IEbayFinancesClient
{
    Task<EbayApiPage<EbayTransaction>> GetTransactionsAsync(EbayTransactionQuery query, CancellationToken cancellationToken = default);
    Task<EbayApiPage<EbayPayout>> GetPayoutsAsync(EbayPayoutQuery query, CancellationToken cancellationToken = default);
}
