namespace ProjectNamePlaceholder.Domain.Entities;

/// <summary>Links a user to an identity at an external sign-in provider (Google, GitHub, ...).</summary>
public sealed class ExternalLogin : BaseEntity
{
    public long UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    /// <summary>The provider's stable, unique id for the user (e.g. the OIDC "sub" claim).</summary>
    public string ProviderKey { get; private set; } = string.Empty;
    public User? User { get; private set; }

    private ExternalLogin() { }

    public ExternalLogin(long userId, string provider, string providerKey)
    {
        UserId = userId;
        Provider = !string.IsNullOrWhiteSpace(provider) ? provider : throw new ArgumentException("Provider is required.", nameof(provider));
        ProviderKey = !string.IsNullOrWhiteSpace(providerKey) ? providerKey : throw new ArgumentException("Provider key is required.", nameof(providerKey));
    }

    public ExternalLogin(User user, string provider, string providerKey) : this(0, provider, providerKey)
    {
        User = user;
    }
}
