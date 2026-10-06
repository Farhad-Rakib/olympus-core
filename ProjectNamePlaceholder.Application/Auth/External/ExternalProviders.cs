namespace ProjectNamePlaceholder.Application.Auth.External;

public sealed record ExternalProviderInfo(string Id, string DisplayName, bool EmailVerifiedByProvider);

/// <summary>The supported sign-in providers.</summary>
public static class ExternalProviders
{
    public const string Google = "google";
    public const string LinkedIn = "linkedin";
    public const string Microsoft = "microsoft";
    public const string GitHub = "github";

    // EmailVerifiedByProvider: whether the provider vouches for email ownership. Only those
    // providers may sign in to an existing account that was matched by email alone.
    public static readonly IReadOnlyList<ExternalProviderInfo> All =
    [
        new(Google, "Google", true),
        new(LinkedIn, "LinkedIn", true),
        new(Microsoft, "Microsoft", false),
        new(GitHub, "GitHub", true),
    ];

    public static ExternalProviderInfo? Find(string? id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Site setting that turns a provider's sign-in button on or off.</summary>
    public static string EnabledSettingKey(string providerId) =>
        $"Auth.{All.First(p => p.Id == providerId).DisplayName}.Enabled";
}
