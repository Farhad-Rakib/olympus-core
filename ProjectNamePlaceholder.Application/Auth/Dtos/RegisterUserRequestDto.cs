namespace ProjectNamePlaceholder.Application.Auth.Dtos;

public sealed record RegisterUserRequestDto(
    string FullName,
    string Email,
    string Password,
    IReadOnlyList<string> Roles);
