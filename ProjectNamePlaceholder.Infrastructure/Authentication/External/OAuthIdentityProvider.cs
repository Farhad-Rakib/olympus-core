using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Auth.External;
using ProjectNamePlaceholder.Application.Common.Configuration;

namespace ProjectNamePlaceholder.Infrastructure.Authentication.External;

/// <summary>
/// OAuth 2.0 authorization-code client (with PKCE) for one provider. Endpoints default to the
/// provider's public ones and can be overridden in configuration.
/// </summary>
public sealed class OAuthIdentityProvider : IExternalIdentityProvider
{
    public const string HttpClientName = "external-auth";

    private sealed record Defaults(string AuthorizationEndpoint, string TokenEndpoint, string UserInfoEndpoint, string Scopes);

    private const string GitHubEmailsEndpoint = "https://api.github.com/user/emails";

    private static readonly Dictionary<string, Defaults> KnownProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        [ExternalProviders.Google] = new(
            "https://accounts.google.com/o/oauth2/v2/auth",
            "https://oauth2.googleapis.com/token",
            "https://openidconnect.googleapis.com/v1/userinfo",
            "openid email profile"),
        [ExternalProviders.LinkedIn] = new(
            "https://www.linkedin.com/oauth/v2/authorization",
            "https://www.linkedin.com/oauth/v2/accessToken",
            "https://api.linkedin.com/v2/userinfo",
            "openid profile email"),
        [ExternalProviders.Microsoft] = new(
            "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            "https://graph.microsoft.com/oidc/userinfo",
            "openid email profile"),
        [ExternalProviders.GitHub] = new(
            "https://github.com/login/oauth/authorize",
            "https://github.com/login/oauth/access_token",
            "https://api.github.com/user",
            "read:user user:email"),
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OAuthProviderOptions _options;
    private readonly Defaults _endpoints;
    private readonly string _emailsEndpoint;

    public OAuthIdentityProvider(string providerId, IHttpClientFactory httpClientFactory, IOptions<ExternalAuthOptions> options)
    {
        ProviderId = providerId;
        _httpClientFactory = httpClientFactory;
        _options = options.Value.Providers.TryGetValue(providerId, out var configured) ? configured : new OAuthProviderOptions();

        var defaults = KnownProviders[providerId];
        _endpoints = new Defaults(
            _options.AuthorizationEndpoint ?? defaults.AuthorizationEndpoint,
            _options.TokenEndpoint ?? defaults.TokenEndpoint,
            _options.UserInfoEndpoint ?? defaults.UserInfoEndpoint,
            _options.Scopes ?? defaults.Scopes);
        _emailsEndpoint = _options.EmailsEndpoint ?? GitHubEmailsEndpoint;
    }

    public string ProviderId { get; }

    public bool IsConfigured => _options.IsConfigured;

    public string BuildAuthorizationUrl(string redirectUri, string state, string codeChallenge)
    {
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = _options.ClientId!,
            ["redirect_uri"] = redirectUri,
            ["scope"] = _endpoints.Scopes,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
        };
        var separator = _endpoints.AuthorizationEndpoint.Contains('?') ? '&' : '?';
        return _endpoints.AuthorizationEndpoint + separator +
               string.Join('&', query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
    }

    public async Task<ExternalUserInfo> GetUserInfoAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default)
    {
        var http = _httpClientFactory.CreateClient(HttpClientName);
        var accessToken = await ExchangeCodeAsync(http, code, redirectUri, codeVerifier, cancellationToken);

        using var profile = await GetJsonAsync(http, _endpoints.UserInfoEndpoint, accessToken, cancellationToken);
        var root = profile.RootElement;

        if (string.Equals(ProviderId, ExternalProviders.GitHub, StringComparison.OrdinalIgnoreCase))
        {
            return await MapGitHubAsync(http, root, accessToken, cancellationToken);
        }

        // OpenID Connect userinfo (Google, LinkedIn, Microsoft).
        var subject = GetString(root, "sub") ?? throw new InvalidOperationException("Provider returned no subject.");
        return new ExternalUserInfo(
            subject,
            GetString(root, "email"),
            GetBool(root, "email_verified"),
            GetString(root, "name"),
            GetString(root, "picture"));
    }

    private async Task<string> ExchangeCodeAsync(HttpClient http, string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoints.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = _options.ClientId!,
                ["client_secret"] = _options.ClientSecret!,
                ["code_verifier"] = codeVerifier,
            }),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return GetString(json.RootElement, "access_token") ?? throw new InvalidOperationException("Provider returned no access token.");
    }

    private async Task<ExternalUserInfo> MapGitHubAsync(HttpClient http, JsonElement user, string accessToken, CancellationToken cancellationToken)
    {
        var id = user.TryGetProperty("id", out var idElement) ? idElement.ToString() : throw new InvalidOperationException("GitHub returned no id.");

        // The profile email may be hidden or unverified; use the primary verified address instead.
        using var emails = await GetJsonAsync(http, _emailsEndpoint, accessToken, cancellationToken);
        var primary = emails.RootElement.EnumerateArray()
            .FirstOrDefault(e => GetBool(e, "primary") && GetBool(e, "verified"));

        return new ExternalUserInfo(
            id,
            primary.ValueKind == JsonValueKind.Object ? GetString(primary, "email") : null,
            primary.ValueKind == JsonValueKind.Object,
            GetString(user, "name") ?? GetString(user, "login"),
            GetString(user, "avatar_url"));
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient http, string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    // Some providers send booleans as strings ("true").
    private static bool GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        (value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var b) && b));
}
