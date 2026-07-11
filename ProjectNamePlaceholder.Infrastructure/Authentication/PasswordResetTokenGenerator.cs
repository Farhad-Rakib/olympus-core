using System.Security.Cryptography;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using Microsoft.Extensions.Options;

namespace ProjectNamePlaceholder.Infrastructure.Authentication;

public sealed class PasswordResetTokenGenerator : IPasswordResetTokenGenerator
{
    private const int TokenExpiryHours = 24;

    public PasswordResetTokenGenerationResult Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(randomBytes);
        var expiresAtUtc = DateTime.UtcNow.AddHours(TokenExpiryHours);

        return new PasswordResetTokenGenerationResult(token, expiresAtUtc);
    }
}
