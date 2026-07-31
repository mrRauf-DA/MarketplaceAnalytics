using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MarketplaceAnalytics.Infrastructure.Persistence.Conventions;

internal sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
