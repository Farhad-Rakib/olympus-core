namespace ProjectNamePlaceholder.Application.Auth.Dtos;

public sealed record LoginResponseDto(string AccessToken, DateTime ExpiresAtUtc);
