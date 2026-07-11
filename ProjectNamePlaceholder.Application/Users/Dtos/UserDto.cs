namespace ProjectNamePlaceholder.Application.Users.Dtos;

public sealed record UserDto(
    long Id,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
