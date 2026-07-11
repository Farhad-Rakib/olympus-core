namespace ProjectNamePlaceholder.Application.Common.Interfaces.Security;

public sealed record RefreshTokenGenerationResult(string Token, DateTime ExpiresAtUtc);
