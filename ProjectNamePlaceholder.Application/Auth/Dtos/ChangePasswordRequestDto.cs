namespace ProjectNamePlaceholder.Application.Auth.Dtos;

public sealed record ChangePasswordRequestDto(long UserId, string CurrentPassword, string NewPassword);
