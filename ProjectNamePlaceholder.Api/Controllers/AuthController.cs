using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Auth;
using ProjectNamePlaceholder.Application.Auth.Dtos;

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

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RegisterUserRequestDto> registerValidator,
        IValidator<RefreshTokenRequestDto> refreshTokenValidator,
        IValidator<RevokeRefreshTokenRequestDto> revokeRefreshTokenValidator,
        IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordValidator,
        IValidator<ChangePasswordRequestDto> changePasswordValidator)
    {
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

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _changePasswordValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authService.ChangePasswordAsync(request, cancellationToken);

        return Ok(ApiResponse.SuccessResponse("Password changed successfully", StatusCodes.Status200OK));
    }
}
