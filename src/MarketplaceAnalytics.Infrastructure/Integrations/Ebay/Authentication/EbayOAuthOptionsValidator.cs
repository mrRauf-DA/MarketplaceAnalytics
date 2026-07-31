using Microsoft.Extensions.Options;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayOAuthOptionsValidator : IValidateOptions<EbayOAuthOptions>
{
    private const int MaximumTimeoutSeconds = 300;

    public ValidateOptionsResult Validate(string? name, EbayOAuthOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (!Enum.IsDefined(options.Environment))
        {
            failures.Add("Environment must be Sandbox or Production.");
        }

        AddRequiredFailure(options.ClientId, "ClientId", failures);
        AddRequiredFailure(options.ClientSecret, "ClientSecret", failures);
        AddRequiredFailure(options.RedirectUriName, "RedirectUriName", failures);

        if (options.DefaultScopes.Count == 0)
        {
            failures.Add("At least one DefaultScopes entry is required.");
        }
        else if (options.DefaultScopes.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("DefaultScopes entries must not be empty.");
        }

        if (options.RequestTimeoutSeconds is < 1 or > MaximumTimeoutSeconds)
        {
            failures.Add("RequestTimeoutSeconds must be between 1 and 300.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddRequiredFailure(
        string value,
        string propertyName,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{propertyName} is required when eBay authentication is enabled.");
        }
    }
}
