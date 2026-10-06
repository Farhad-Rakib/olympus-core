using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Auth.Dtos;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Auth.External;

public sealed class ExternalAuthService : IExternalAuthService
{
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LoginCodeTtl = TimeSpan.FromMinutes(1);

    /// <summary>Error codes passed to the frontend's /login?error=... (never provider details).</summary>
    public static class Errors
    {
        public const string Expired = "external_expired";
        public const string Cancelled = "external_cancelled";
        public const string Unavailable = "external_unavailable";
        public const string NoEmail = "external_no_email";
        public const string EmailInUse = "external_email_in_use";
        public const string Inactive = "external_inactive";
        public const string Failed = "external_failed";
    }

    private sealed record PendingSignIn(string ProviderId, string CodeVerifier, string RedirectUri, string ReturnUrl);
    private sealed record PendingLogin(long UserId);

    private readonly IEnumerable<IExternalIdentityProvider> _providers;
    private readonly ISiteSettingRepository _siteSettings;
    private readonly IUserRepository _userRepository;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthService _authService;
    private readonly IAppCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ExternalAuthOptions _options;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IEnumerable<IExternalIdentityProvider> providers,
        ISiteSettingRepository siteSettings,
        IUserRepository userRepository,
        IExternalLoginRepository externalLoginRepository,
        IPasswordHasher passwordHasher,
        IAuthService authService,
        IAppCache cache,
        IUnitOfWork unitOfWork,
        IOptions<ExternalAuthOptions> options,
        ILogger<ExternalAuthService> logger)
    {
        _providers = providers;
        _siteSettings = siteSettings;
        _userRepository = userRepository;
        _externalLoginRepository = externalLoginRepository;
        _passwordHasher = passwordHasher;
        _authService = authService;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalProviderDto>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await GetProviderStatusesAsync(cancellationToken);
        return statuses.Where(s => s.Enabled && s.Configured).Select(s => new ExternalProviderDto(s.Id, s.DisplayName)).ToList();
    }

    public async Task<IReadOnlyList<ExternalProviderStatusDto>> GetProviderStatusesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ExternalProviderStatusDto>();
        foreach (var info in ExternalProviders.All)
        {
            var key = ExternalProviders.EnabledSettingKey(info.Id);
            var setting = await _siteSettings.GetByKeyAsync(key, cancellationToken);
            var enabled = bool.TryParse(setting?.Value, out var on) && on;
            var configured = GetClient(info.Id)?.IsConfigured ?? false;
            result.Add(new ExternalProviderStatusDto(info.Id, info.DisplayName, enabled, configured, key));
        }
        return result;
    }

    public async Task<string> StartAsync(string providerId, string redirectUri, string? returnUrl, CancellationToken cancellationToken = default)
    {
        var client = await GetAvailableClientAsync(providerId, cancellationToken)
            ?? throw new NotFoundException("This sign-in method is not available.");

        var state = RandomToken();
        var codeVerifier = RandomToken();
        await _cache.SetAsync(StateKey(state),
            new PendingSignIn(client.ProviderId, codeVerifier, redirectUri, SafeReturnUrl(returnUrl)), StateTtl, cancellationToken);

        return client.BuildAuthorizationUrl(redirectUri, state, CodeChallenge(codeVerifier));
    }

    public async Task<string> HandleCallbackAsync(string providerId, string? code, string? state, string? error, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(state)) return LoginErrorUrl(Errors.Expired);

        // State is single-use: it binds this callback to the browser that started the sign-in.
        var pending = await _cache.GetAsync<PendingSignIn>(StateKey(state), cancellationToken);
        await _cache.RemoveAsync(StateKey(state), cancellationToken);
        if (pending is null || !string.Equals(pending.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
        {
            return LoginErrorUrl(Errors.Expired);
        }

        if (!string.IsNullOrEmpty(error) || string.IsNullOrWhiteSpace(code)) return LoginErrorUrl(Errors.Cancelled);

        var client = await GetAvailableClientAsync(providerId, cancellationToken);
        if (client is null) return LoginErrorUrl(Errors.Unavailable);

        ExternalUserInfo identity;
        try
        {
            identity = await client.GetUserInfoAsync(code, pending.RedirectUri, pending.CodeVerifier, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "External sign-in with {Provider} failed", providerId);
            return LoginErrorUrl(Errors.Failed);
        }

        var (user, failure) = await ResolveUserAsync(client.ProviderId, identity, cancellationToken);
        if (user is null) return LoginErrorUrl(failure!);

        var loginCode = RandomToken();
        await _cache.SetAsync(LoginCodeKey(loginCode), new PendingLogin(user.Id), LoginCodeTtl, cancellationToken);

        return $"{FrontendBase}/auth/callback?code={Uri.EscapeDataString(loginCode)}&returnUrl={Uri.EscapeDataString(pending.ReturnUrl)}";
    }

    public async Task<AuthTokensDto> ExchangeLoginCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var pending = await _cache.GetAsync<PendingLogin>(LoginCodeKey(code), cancellationToken);
        await _cache.RemoveAsync(LoginCodeKey(code), cancellationToken);
        if (pending is null) throw new AppException("Sign-in link is invalid or has expired.", 401);

        return await _authService.CreateSessionAsync(pending.UserId, cancellationToken);
    }

    private async Task<(User? User, string? Error)> ResolveUserAsync(string providerId, ExternalUserInfo identity, CancellationToken cancellationToken)
    {
        // 1. Returning user: this provider identity is already linked.
        var link = await _externalLoginRepository.FindAsync(providerId, identity.ProviderKey, cancellationToken);
        if (link is not null)
        {
            var linked = await _userRepository.GetByIdAsync(link.UserId, cancellationToken);
            return linked is { IsActive: true } ? (linked, null) : (null, Errors.Inactive);
        }

        if (string.IsNullOrWhiteSpace(identity.Email)) return (null, Errors.NoEmail);

        var existing = await _userRepository.GetByEmailAsync(identity.Email, cancellationToken);
        if (existing is not null)
        {
            // 2. Existing account with this email: link only if the provider proved ownership of the
            //    address, otherwise anyone could claim an account by using its email elsewhere.
            var trusted = ExternalProviders.Find(providerId)?.EmailVerifiedByProvider == true && identity.EmailVerified;
            if (!trusted) return (null, Errors.EmailInUse);
            if (!existing.IsActive) return (null, Errors.Inactive);

            await _externalLoginRepository.AddAsync(new ExternalLogin(existing.Id, providerId, identity.ProviderKey), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return (existing, null);
        }

        // 3. New user. Like self-registration, they start without roles. The random password is
        //    never shown; they can set a real one later through "Forgot password".
        var user = new User(
            string.IsNullOrWhiteSpace(identity.Name) ? identity.Email : identity.Name,
            identity.Email,
            _passwordHasher.Hash(RandomToken()));
        if (!string.IsNullOrWhiteSpace(identity.PictureUrl))
        {
            user.UpdateProfile(user.FullName, user.Email, identity.PictureUrl);
        }

        await _userRepository.AddAsync(user, cancellationToken);
        await _externalLoginRepository.AddAsync(new ExternalLogin(user, providerId, identity.ProviderKey), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (user, null);
    }

    private async Task<IExternalIdentityProvider?> GetAvailableClientAsync(string providerId, CancellationToken cancellationToken)
    {
        var info = ExternalProviders.Find(providerId);
        if (info is null) return null;

        var client = GetClient(info.Id);
        if (client is not { IsConfigured: true }) return null;

        var setting = await _siteSettings.GetByKeyAsync(ExternalProviders.EnabledSettingKey(info.Id), cancellationToken);
        return bool.TryParse(setting?.Value, out var on) && on ? client : null;
    }

    private IExternalIdentityProvider? GetClient(string providerId) =>
        _providers.FirstOrDefault(p => string.Equals(p.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));

    private string FrontendBase => _options.FrontendBaseUrl.TrimEnd('/');

    private string LoginErrorUrl(string error) => $"{FrontendBase}/login?error={error}";

    /// <summary>Only same-site relative paths, so the flow cannot be used as an open redirect.</summary>
    private static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/dashboard";

    private static string StateKey(string state) => $"external-auth:state:{state}";
    private static string LoginCodeKey(string code) => $"external-auth:code:{code}";

    private static string RandomToken() => Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string CodeChallenge(string codeVerifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
