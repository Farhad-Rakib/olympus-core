using ProjectNamePlaceholder.Application.Auth.Dtos;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using ProjectNamePlaceholder.Application.Common.Interfaces.Services;
using ProjectNamePlaceholder.Application.Users.Dtos;
using ProjectNamePlaceholder.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace ProjectNamePlaceholder.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IPasswordResetTokenGenerator _passwordResetTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IPasswordResetTokenGenerator passwordResetTokenGenerator,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _passwordResetTokenGenerator = passwordResetTokenGenerator;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokensDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash) || !user.IsActive)
        {
            throw new AppException("Invalid credentials.", 401);
        }

        var permissions = await _permissionRepository.GetPermissionNamesForUserAsync(user.Id, cancellationToken);
        return await GenerateAuthTokensAsync(user, permissions, cancellationToken);
    }

    public async Task<AuthTokensDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeSha256(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new AppException("Invalid refresh token.", 401);
        }

        var user = await _userRepository.GetByIdWithRolesAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AppException("User is not active.", 401);
        }

        var permissions = await _permissionRepository.GetPermissionNamesForUserAsync(user.Id, cancellationToken);

        var refreshToken = _refreshTokenGenerator.Generate();
        var newTokenHash = ComputeSha256(refreshToken.Token);
        existingToken.Revoke(newTokenHash);
        _refreshTokenRepository.Update(existingToken);

        await _refreshTokenRepository.AddAsync(new RefreshToken(user.Id, newTokenHash, refreshToken.ExpiresAtUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, permissions);

        return new AuthTokensDto(
            accessToken,
            refreshToken.Token,
            _jwtTokenGenerator.GetAccessTokenExpiryUtc(),
            refreshToken.ExpiresAtUtc);
    }

    public async Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeSha256(request.RefreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            return;
        }

        token.Revoke();
        _refreshTokenRepository.Update(token);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserRegistrationResult> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new AppException("Email is already registered.");
        }

        var hashedPassword = _passwordHasher.Hash(request.Password);
        var user = new User(request.FullName, request.Email, hashedPassword);

        var roles = await _roleRepository.GetByNamesAsync(request.Roles, cancellationToken);
        user.SetRoles(roles.Select(role => new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        }));

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserRegistrationResult(new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            roles.Select(r => r.Name).ToList()));
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(Dtos.ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        
        if (user is null || !user.IsActive)
        {
            // Don't reveal if user exists (security best practice)
            return new ForgotPasswordResponseDto("If an account exists with this email, a password reset link has been sent.");
        }

        // Generate password reset token
        var resetToken = _passwordResetTokenGenerator.Generate();
        var resetTokenHash = ComputeSha256(resetToken.Token);

        // Save the reset token
        await _passwordResetTokenRepository.AddAsync(
            new PasswordResetToken(user.Id, resetTokenHash, resetToken.ExpiresAtUtc),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email with reset link
        var resetLink = $"https://localhost:3000/reset-password?token={resetToken.Token}&email={Uri.EscapeDataString(user.Email)}";
        var htmlBody = $@"
            <h2>Password Reset Request</h2>
            <p>Hi {user.FullName},</p>
            <p>You requested a password reset. Click the link below to reset your password:</p>
            <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a></p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't request this, please ignore this email.</p>
        ";

        await _emailService.SendAsync(user.Email, "Password Reset Request", htmlBody, cancellationToken);

        return new ForgotPasswordResponseDto("If an account exists with this email, a password reset link has been sent.");
    }

    public async Task ResetPasswordAsync(Dtos.ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new AppException("Invalid reset token or password.", 400);
        }

        var tokenHash = ComputeSha256(request.Token);
        var resetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsValid)
        {
            throw new AppException("Invalid or expired reset token.", 400);
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.Id != resetToken.UserId)
        {
            throw new AppException("Invalid reset token or email.", 400);
        }

        // Update password
        var hashedPassword = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(hashedPassword);
        
        // Mark reset token as used
        resetToken.MarkAsUsed();
        _passwordResetTokenRepository.Update(resetToken);
        
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send confirmation email
        var htmlBody = $@"
            <h2>Password Reset Successful</h2>
            <p>Hi {user.FullName},</p>
            <p>Your password has been successfully reset.</p>
            <p>If you didn't make this change, please contact support immediately.</p>
        ";

        await _emailService.SendAsync(user.Email, "Password Reset Confirmation", htmlBody, cancellationToken);
    }

    public async Task ChangePasswordAsync(Dtos.ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user is null || !user.IsActive)
        {
            throw new AppException("User not found or is inactive.", 404);
        }

        // Verify current password
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AppException("Current password is incorrect.", 401);
        }

        // Ensure new password is different from current
        if (request.CurrentPassword == request.NewPassword)
        {
            throw new AppException("New password must be different from current password.", 400);
        }

        // Update password
        var hashedPassword = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(hashedPassword);
        
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send confirmation email
        var htmlBody = $@"
            <h2>Password Changed</h2>
            <p>Hi {user.FullName},</p>
            <p>Your password has been successfully changed.</p>
            <p>If you didn't make this change, please contact support immediately.</p>
        ";

        await _emailService.SendAsync(user.Email, "Password Changed", htmlBody, cancellationToken);
    }

    private async Task<AuthTokensDto> GenerateAuthTokensAsync(User user, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, permissions);
        var refreshToken = _refreshTokenGenerator.Generate();
        var refreshTokenHash = ComputeSha256(refreshToken.Token);

        await _refreshTokenRepository.AddAsync(new RefreshToken(user.Id, refreshTokenHash, refreshToken.ExpiresAtUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(
            accessToken,
            refreshToken.Token,
            _jwtTokenGenerator.GetAccessTokenExpiryUtc(),
            refreshToken.ExpiresAtUtc);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
