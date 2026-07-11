using System.Security.Cryptography;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using Microsoft.Extensions.Options;

namespace ProjectNamePlaceholder.Infrastructure.Authentication;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public RefreshTokenGenerationResult Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

        return new RefreshTokenGenerationResult(token, expiresAtUtc);
    }
}
