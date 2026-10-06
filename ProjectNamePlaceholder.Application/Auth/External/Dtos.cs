namespace ProjectNamePlaceholder.Application.Auth.External;

/// <summary>A sign-in button the login page should show.</summary>
public sealed record ExternalProviderDto(string Id, string DisplayName);

/// <summary>Admin view: whether a provider is switched on and whether the server has credentials for it.</summary>
public sealed record ExternalProviderStatusDto(string Id, string DisplayName, bool Enabled, bool Configured, string SettingKey);

public sealed record ExternalLoginExchangeRequestDto(string Code);
