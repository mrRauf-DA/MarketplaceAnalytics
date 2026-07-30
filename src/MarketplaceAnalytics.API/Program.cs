using MarketplaceAnalytics.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketplaceAnalyticsConfiguration(builder.Configuration);

var app = builder.Build();

app.Run();
