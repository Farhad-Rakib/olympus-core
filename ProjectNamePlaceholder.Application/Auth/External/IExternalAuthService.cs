using ProjectNamePlaceholder.Application.Auth.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.External;

public interface IExternalAuthService
{
    /// <summary>Providers that are switched on in site settings and configured on the server.</summary>
    Task<IReadOnlyList<ExternalProviderDto>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalProviderStatusDto>> GetProviderStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the provider URL to send the browser to.</summary>
    Task<string> StartAsync(string providerId, string redirectUri, string? returnUrl, CancellationToken cancellationToken = default);

    /// <summary>Completes the provider callback and returns the frontend URL to send the browser to.</summary>
    Task<string> HandleCallbackAsync(string providerId, string? code, string? state, string? error, CancellationToken cancellationToken = default);

    /// <summary>Trades the one-time login code from the callback for API tokens.</summary>
    Task<AuthTokensDto> ExchangeLoginCodeAsync(string code, CancellationToken cancellationToken = default);
}
