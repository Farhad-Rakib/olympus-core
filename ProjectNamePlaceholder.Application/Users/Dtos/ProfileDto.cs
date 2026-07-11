namespace ProjectNamePlaceholder.Application.Users.Dtos;

public sealed record ProfileDto(
    long Id,
    string FullName,
    string Email,
    bool IsActive,
    string? ProfileImageUrl,
    IReadOnlyList<string> Roles
);
