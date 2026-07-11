namespace ProjectNamePlaceholder.Application.Roles.Dtos;

public sealed record RoleDto(
    long Id,
    string Name,
    string Description,
    IReadOnlyList<string> Permissions);