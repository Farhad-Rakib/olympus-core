namespace ProjectNamePlaceholder.Application.Auth.Dtos;

public sealed record ResetPasswordRequestDto(string Email, string Token, string NewPassword);
