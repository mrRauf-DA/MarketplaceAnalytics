using MarketplaceAnalytics.API.Configuration;
using MarketplaceAnalytics.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketplaceAnalyticsConfiguration(builder.Configuration);
builder.Services.AddMarketplaceAnalyticsPersistence(builder.Configuration);

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
