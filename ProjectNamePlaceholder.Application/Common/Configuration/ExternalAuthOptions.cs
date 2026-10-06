namespace ProjectNamePlaceholder.Application.Common.Configuration;

/// <summary>Settings for external sign-in ("ExternalAuth" section).</summary>
public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    /// <summary>Where the browser is sent after a provider callback, e.g. http://localhost:5173.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>
    /// Public API origin used to build provider callback URLs, e.g. https://api.example.com.
    /// Leave empty to use the incoming request's origin (set it when running behind a proxy).
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>Per-provider credentials and (optionally overridden) endpoints, keyed by provider id.</summary>
    public Dictionary<string, OAuthProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class OAuthProviderOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? UserInfoEndpoint { get; set; }
    /// <summary>GitHub only: the endpoint listing the user's email addresses.</summary>
    public string? EmailsEndpoint { get; set; }
    public string? Scopes { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
