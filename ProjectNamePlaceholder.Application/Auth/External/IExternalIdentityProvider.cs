namespace ProjectNamePlaceholder.Application.Auth.External;

/// <summary>The identity a provider returned after a successful sign-in.</summary>
public sealed record ExternalUserInfo(string ProviderKey, string? Email, bool EmailVerified, string? Name, string? PictureUrl);

/// <summary>An OAuth 2.0 authorization-code client for one provider.</summary>
public interface IExternalIdentityProvider
{
    string ProviderId { get; }
    bool IsConfigured { get; }
    string BuildAuthorizationUrl(string redirectUri, string state, string codeChallenge);
    Task<ExternalUserInfo> GetUserInfoAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default);
}
