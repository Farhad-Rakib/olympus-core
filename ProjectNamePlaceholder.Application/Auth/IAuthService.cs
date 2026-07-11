using ProjectNamePlaceholder.Application.Auth.Dtos;

namespace ProjectNamePlaceholder.Application.Auth;

public interface IAuthService
{
    Task<AuthTokensDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokensDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<UserRegistrationResult> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
}
