using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Auth;
using ProjectNamePlaceholder.Application.Auth.Dtos;
using ProjectNamePlaceholder.Application.Auth.External;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Security;
using Microsoft.Extensions.Options;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<RegisterUserRequestDto> _registerValidator;
    private readonly IValidator<RefreshTokenRequestDto> _refreshTokenValidator;
    private readonly IValidator<RevokeRefreshTokenRequestDto> _revokeRefreshTokenValidator;
    private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;
    private readonly IValidator<ChangePasswordRequestDto> _changePasswordValidator;
    private readonly IExternalAuthService _externalAuthService;
    private readonly ExternalAuthOptions _externalAuthOptions;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RegisterUserRequestDto> registerValidator,
        IValidator<RefreshTokenRequestDto> refreshTokenValidator,
        IValidator<RevokeRefreshTokenRequestDto> revokeRefreshTokenValidator,
        IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordValidator,
        IValidator<ChangePasswordRequestDto> changePasswordValidator,
        IExternalAuthService externalAuthService,
        IOptions<ExternalAuthOptions> externalAuthOptions)
    {
        _externalAuthService = externalAuthService;
        _externalAuthOptions = externalAuthOptions.Value;
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _revokeRefreshTokenValidator = revokeRefreshTokenValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        var response = await _authService.LoginAsync(request, cancellationToken);

        return Ok(ApiResponse<AuthTokensDto>.SuccessResponse(response, "Login successful", StatusCodes.Status200OK));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto request, CancellationToken cancellationToken)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _authService.RegisterAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Register), new { id = result.User.Id }, ApiResponse<dynamic>.SuccessResponse(result, "User registered successfully", StatusCodes.Status201Created));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        await _refreshTokenValidator.ValidateAndThrowAsync(request, cancellationToken);
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);

        return Ok(ApiResponse<AuthTokensDto>.SuccessResponse(response, "Token refreshed successfully", StatusCodes.Status200OK));
    }

    [AllowAnonymous]
    [HttpPost("revoke-refresh")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeRefresh([FromBody] RevokeRefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        await _revokeRefreshTokenValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authService.RevokeRefreshTokenAsync(request, cancellationToken);

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _forgotPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);
        var response = await _authService.ForgotPasswordAsync(request, cancellationToken);

        return Ok(ApiResponse<ForgotPasswordResponseDto>.SuccessResponse(response, "Password reset email sent successfully", StatusCodes.Status200OK));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _resetPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authService.ResetPasswordAsync(request, cancellationToken);

        return Ok(ApiResponse.SuccessResponse("Password reset successfully", StatusCodes.Status200OK));
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        // Always act on the authenticated user, never on a client-supplied id.
        if (!long.TryParse(User.FindFirst("sub")?.Value, out var currentUserId))
        {
            return Unauthorized(ApiResponse.FailureResponse("Unauthorized.", StatusCodes.Status401Unauthorized));
        }

        request = request with { UserId = currentUserId };
        await _changePasswordValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authService.ChangePasswordAsync(request, cancellationToken);

        return Ok(ApiResponse.SuccessResponse("Password changed successfully", StatusCodes.Status200OK));
    }

    /// <summary>Sign-in providers to show on the login page (switched on and configured).</summary>
    [AllowAnonymous]
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExternalProviderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
    {
        var providers = await _externalAuthService.GetAvailableProvidersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ExternalProviderDto>>.SuccessResponse(providers, "Sign-in providers retrieved"));
    }

    /// <summary>Every provider with its on/off flag and whether the server has credentials for it.</summary>
    [Authorize(Policy = Permissions.SiteSettingsRead)]
    [HttpGet("providers/status")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExternalProviderStatusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderStatuses(CancellationToken cancellationToken)
    {
        var statuses = await _externalAuthService.GetProviderStatusesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ExternalProviderStatusDto>>.SuccessResponse(statuses, "Sign-in provider status retrieved"));
    }

    /// <summary>Starts external sign-in by redirecting the browser to the provider.</summary>
    [AllowAnonymous]
    [HttpGet("external/{provider}/start")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartExternal(string provider, [FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        var url = await _externalAuthService.StartAsync(provider, ExternalCallbackUrl(provider), returnUrl, cancellationToken);
        return Redirect(url);
    }

    /// <summary>Provider callback; finishes sign-in and redirects the browser back to the app.</summary>
    [AllowAnonymous]
    [HttpGet("external/{provider}/callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> ExternalCallback(
        string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var url = await _externalAuthService.HandleCallbackAsync(provider, code, state, error, cancellationToken);
        return Redirect(url);
    }

    /// <summary>Exchanges the one-time code from the callback redirect for tokens.</summary>
    [AllowAnonymous]
    [HttpPost("external/exchange")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExchangeExternal([FromBody] ExternalLoginExchangeRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(ApiResponse.FailureResponse("Code is required.", StatusCodes.Status400BadRequest));
        }

        var tokens = await _externalAuthService.ExchangeLoginCodeAsync(request.Code, cancellationToken);
        return Ok(ApiResponse<AuthTokensDto>.SuccessResponse(tokens, "Login successful", StatusCodes.Status200OK));
    }

    private string ExternalCallbackUrl(string provider)
    {
        var origin = string.IsNullOrWhiteSpace(_externalAuthOptions.ApiBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : _externalAuthOptions.ApiBaseUrl.TrimEnd('/');
        return $"{origin}/api/v1/auth/external/{provider.ToLowerInvariant()}/callback";
    }
}
