namespace ProjectNamePlaceholder.Application.Common.Interfaces.Security;

public sealed record PasswordResetTokenGenerationResult(string Token, DateTime ExpiresAtUtc);
