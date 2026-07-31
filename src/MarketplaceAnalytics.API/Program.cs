using MarketplaceAnalytics.API.Configuration;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api;
using MarketplaceAnalytics.Infrastructure.Persistence;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketplaceAnalyticsConfiguration(builder.Configuration);
builder.Services.AddMarketplaceAnalyticsPersistence(builder.Configuration);
builder.Services.AddEbayAuthentication(builder.Configuration);
builder.Services.AddEbayApiClients();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
